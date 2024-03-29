﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;
using LDD.Core.Primitives;
using LDD.Core.Primitives.Connectors;
using LDD.Common.Utilities;

namespace LDD.Modding
{
    [XmlRoot("Connection")]
    public /*abstract*/ class PartConnection : PartElement, IPhysicalElement, IClonableElement
    {
        public const string NODE_NAME = "Connection";

        private ItemTransform _Transform;
        private Connector _Connector;

        public ItemTransform Transform
        {
            get => _Transform;
            set => SetPropertyValue(ref _Transform, value);
        }

        //public dynamic ConnectorProxy { get; private set; }
        public bool IsAssigningConnectorProperties { get; private set; }

        [XmlIgnore]
        public Connector Connector
        {
            get => _Connector/* != null ? _ConnectorProxy : null*/;
            //set => SetConnector(value);
            set => SetPropertyValue(ref _Connector, value);
        }

        [XmlAttribute]
        public ConnectorType ConnectorType { get; set; }

        public int SubType
        {
            get => Connector?.SubType ?? 0;
            set {
                if (Connector != null) 
                    Connector.SubType = value;
            }
        }

        public event EventHandler TranformChanged;

        public PartConnection()
        {
            Transform = new ItemTransform();
        }

        protected PartConnection(ConnectorType connectorType)
        {
            Transform = new ItemTransform();
            ConnectorType = connectorType;
        }

        public PartConnection(Connector connector)
        {
            ConnectorType = connector.Type;
            //SetConnector(connector);
            _Connector = connector;
            Transform = ItemTransform.FromLDD(connector.Transform);
        }

        protected override void OnPropertyValueChanged(PropertyValueChangedEventArgs args)
        {
            base.OnPropertyValueChanged(args);
            if (args.PropertyName == nameof(Transform))
                TranformChanged?.Invoke(this, EventArgs.Empty);
        }


        //protected override void OnPropertyChanged(ElementValueChangedEventArgs args)
        //{
        //    base.OnPropertyChanged(args);
        //    if (args.PropertyName == nameof(Transform))
        //    {
        //        //if (args.OldValue is ItemTransform oldTransform)
        //        //{
        //        //    oldTransform.
        //        //}
        //        //Trace.WriteLine("PartConnection.OnPropertyChanged: Transform");
        //        TranformChanged?.Invoke(this, EventArgs.Empty);
        //    }
        //}

        #region Connector Handling

        //private void SetConnector(Connector connector)
        //{
        //    if (_Connector != null)
        //    {
        //        _Connector.PropertyValueChanged -= PartConnection_PropertyValueChanged;
        //        _Connector.ChildEventForwarded -= Connector_ChildEventForwarded;
        //    }

        //    _Connector = connector;

        //    if (_Connector != null)
        //    {
        //        _Connector.PropertyValueChanged += PartConnection_PropertyValueChanged;
        //        _Connector.ChildEventForwarded += Connector_ChildEventForwarded;
        //    }
        //}


        protected override void OnChildPropertyValueChanged(string propertyName, object childObject, PropertyValueChangedEventArgs args)
        {
            base.OnChildPropertyValueChanged(propertyName, childObject, args);
            if (IsAssigningConnectorProperties)
                return;
            RaisePropertyValueChanged(childObject, args);
        }
        //private void Connector_ChildEventForwarded(object sender, ForwardedEventArgs e)
        //{
        //    if (IsAssigningConnectorProperties)
        //        return;

        //    if (e.ForwardedEvent is PropertyValueChangedEventArgs eventArgs)
        //    {
        //        if (e.ChildObject is LDDModder.LDD.Primitives.Transform)
        //        {
        //            return;
        //        }

        //        //var changeArgs = new ElementValueChangedEventArgs(this,
        //        //    e.ChildObject, eventArgs.PropertyName,
        //        //    eventArgs.OldValue, eventArgs.NewValue)
        //        //{
        //        //    Index = eventArgs.Index
        //        //};
        //        RaisePropertyValueChanged(e.ChildObject, eventArgs);
        //    }
        //}

        //private void PartConnection_PropertyValueChanged(object sender, PropertyValueChangedEventArgs e)
        //{
        //    if (IsAssigningConnectorProperties)
        //        return;

        //    //var changeArgs = new ElementValueChangedEventArgs(this, Connector, e.PropertyName, e.OldValue, e.NewValue)
        //    //{
        //    //    Index = e.Index
        //    //};
        //    RaisePropertyValueChanged(Connector, e);
        //    //RaisePropertyValueChanged(changeArgs);
        //}

        #endregion

        public static PartConnection FromLDD(Connector connector)
        {
            return new PartConnection(connector);
        }

        public static PartConnection FromXml(XElement element)
        {
            var connectorType = element.ReadAttribute<ConnectorType>("Type");
            var partConn = new PartConnection(connectorType);
            partConn.LoadFromXml(element);
            return partConn;
        }

        public static PartConnection Create(ConnectorType connectorType)
        {
            var conn = new PartConnection(Connector.CreateFromType(connectorType));
            conn.SetDefaultValues();
            return conn;
        }

        public void SetDefaultValues()
        {
            if (ConnectorType == ConnectorType.Axel)
            {
                Connector.SubType = 5;
                GetConnector<AxelConnector>().Length = 0.8f;
            }
        }

        public T GetConnector<T>() where T : Connector
        {
            return _Connector as T;
        }

        public PartConnection Clone()
        {
            var connClone = Connector.Clone();
            connClone.Transform = Transform.ToLDD();
            return new PartConnection(connClone);
        }

        PartElement IClonableElement.Clone()
        {
            return Clone();
        }

        public Connector GenerateLDD()
        {
            IsAssigningConnectorProperties = true;
            Connector.Transform = Transform.ToLDD();
            IsAssigningConnectorProperties = false;
            return Connector;
        }

        protected internal override void LoadFromXml(XElement element)
        {
            base.LoadFromXml(element);

            if (element.HasElement(nameof(Transform), out XElement transElem))
            {
                Transform = ItemTransform.FromXml(transElem);
                transElem.Remove();
            }

            var connElem = new XElement(ConnectorType.ToString());

            connElem.Add(Transform.ToLDD().ToXmlAttributes());

            foreach (var attr in element.Element("Properties").Attributes())
                connElem.Add(new XAttribute(attr.Name.LocalName, attr.Value));

            if (element.HasElement("StudsArray", out XElement studs))
                connElem.Value = studs.Value;

            IsAssigningConnectorProperties = true;
            Connector = Connector.CreateFromType(ConnectorType);
            Connector.LoadFromXml(connElem);
            IsAssigningConnectorProperties = false;

        }

        public override XElement SerializeToXml()
        {
            var elem = SerializeToXmlBase(NODE_NAME);

            elem.Add(new XAttribute("Type", ConnectorType));

            if (Transform != null)
            {
                elem.Add(new XComment(Transform.GetLddXml().ToString()));
                elem.Add(Transform.SerializeToXml());
            }

            if (Connector != null)
            {
                var connectorXml = Connector.SerializeToXml();

                var propElem = elem.AddElement("Properties");

                foreach (var attr in connectorXml.Attributes())
                {
                    if (LDD.Core.Primitives.Transform.AttributeNames.Contains(attr.Name.LocalName))
                        continue;

                    propElem.Add(new XAttribute(attr.Name.LocalName.Capitalize(), attr.Value));
                    //propElem.AddElement(attr.Name.LocalName.Capitalize(), attr.Value);
                }

                if (!string.IsNullOrEmpty(connectorXml.Value))//Custom2DField
                {
                    elem.AddElement("StudsArray", connectorXml.Value
                        .Replace("\r", string.Empty)
                        .Replace("\n", string.Empty)
                        .Trim()
                    );
                }
            }
            
            return elem;
        }

        
    }
    
}

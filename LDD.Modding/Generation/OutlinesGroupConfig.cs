using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LDD.Modding
{
    public class OutlinesGroupConfig : PartElement
    {
        public const string NODE_NAME = "OutlineGroup";

        private double _AngleThreshold;
        private double _Thickness;

        public double AngleThreshold
        {
            get => _AngleThreshold;
            set => SetPropertyValue(ref _AngleThreshold, value);
        }

        public double Thickness
        {
            get => _Thickness;
            set => SetPropertyValue(ref _Thickness, value);
        }

        public ElementReferenceCollection Elements { get; set; }

        public OutlinesGroupConfig()
        {
            _AngleThreshold = 35;
            _Thickness = 1;
            Elements = new ElementReferenceCollection(this);
            Elements.SupportedTypes.Add(typeof(ModelMeshReference));
            TrackCollectionChanges(Elements);
        }

        public override XElement SerializeToXml()
        {
            var elem = SerializeToXmlBase(NODE_NAME);

            elem.WriteAttribute(nameof(AngleThreshold), AngleThreshold);
            elem.WriteAttribute(nameof(Thickness), Thickness);

            elem.Add(Elements.Serialize(nameof(Elements)));

            return elem;
        }

        protected internal override void LoadFromXml(XElement element)
        {
            base.LoadFromXml(element);

            AngleThreshold = element.ReadAttribute(nameof(AngleThreshold), 35d);
            Thickness = element.ReadAttribute(nameof(Thickness), 1d);
            Elements.Clear();
            if (element.HasElement(nameof(Elements), out XElement elemsElem))
                Elements.LoadFromXml(elemsElem);
        }

        public static OutlinesGroupConfig FromXml(XElement element)
        {
            var config = new OutlinesGroupConfig();
            config.LoadFromXml(element);
            return config;
        }
    }
}

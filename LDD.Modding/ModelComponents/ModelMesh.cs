﻿using LDD.Core.Meshes;
using LDD.Common.Serialization;
using LDD.Common.Simple3D;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
using LDD.Common.Utilities;

namespace LDD.Modding
{
    public class ModelMesh : PartElement
    {
        public const string NODE_NAME = "Mesh";
        private MeshGeometry _Geometry;
        private XElement GeomtryXml;

        public MeshGeometry Geometry
        {
            get => _Geometry;
            set => SetGeometry(value);
        }

        #region Geometry Attributes

        public bool IsTextured { get; set; }

        public bool IsFlexible { get; set; }

        public int VertexCount { get; set; }

        public int IndexCount { get; set; }

        public int BoneCount { get; set; }

        #endregion

        public string LegacyFilename { get; set; }

        public PartSurface Surface => (Parent as SurfaceComponent)?.Parent as PartSurface;

        /// <summary>
        /// TODO: Move loading/unloading to BrickEditor
        /// </summary>
        public bool GeometrySaved { get; private set; }
        /// <summary>
        /// TODO: Move loading/unloading to BrickEditor
        /// </summary>
        public bool IsModelLoaded => Geometry != null;
        /// <summary>
        /// TODO: Move loading/unloading to BrickEditor
        /// </summary>
        public bool CanUnloadModel => GeometrySaved;

        public ModelMesh()
        {
            ID = StringUtils.GenerateUID(8);
        }

        public ModelMesh(MeshGeometry geometry)
        {
            Geometry = geometry;
            UpdateMeshProperties();
        }

        public IEnumerable<ModelMeshReference> GetReferences()
        {
            if (Project != null)
                return Project.Surfaces.SelectMany(x => x.GetAllMeshReferences()).Where(y => y.MeshID == ID);
            return Enumerable.Empty<ModelMeshReference>();
        }

        private void SetGeometry(MeshGeometry geometry)
        {
            if (_Geometry != geometry)
            {
                _Geometry = geometry;
                if (geometry != null)
                    GeometrySaved = false;
                UpdateMeshProperties();
            }
        }

        #region Xml Serialization

        public override XElement SerializeToXml()
        {
            var elem = SerializeToXmlBase(NODE_NAME);

            XElement geomElem = null;

            if (IsModelLoaded)
            {
                geomElem = Geometry.ConvertToXml().Root;
            }
            else if (GeometrySaved)
            {
                geomElem = GeomtryXml ?? GetGeometryElementFromProjectFile();
                return geomElem;
            }

            if (geomElem != null)
            {
                elem.Add(geomElem.Nodes().ToArray());
                elem.Add(geomElem.Attributes().ToArray());
            }
            else
            {
                Project?.ErrorMessages.Add($"Could not save the model '{Name}' (ID: {ID}). The model was unloaded and could not be retrieved.");
            }
            return elem;
        }

        protected internal override void LoadFromXml(XElement element)
        {
            base.LoadFromXml(element);
            IsTextured = element.ReadAttribute("IsTextured", false);
            IsFlexible = element.ReadAttribute("IsFlexible", false);
            LegacyFilename = element.ReadAttribute("FileName", string.Empty);

            if (element.HasElement("Positions"))
            {
                GeomtryXml = element;
                GeometrySaved = true;
                //_Geometry = GetGeometryFromElement(element);
                //GeometrySaved = _Geometry != null;
            }
        }

        public static ModelMesh FromXml(XElement element)
        {
            var model = new ModelMesh();
            model.LoadFromXml(element);
            return model;
        }

        #endregion

        public void UpdateMeshProperties()
        {
            if (Geometry != null)
            {
                VertexCount = Geometry.VertexCount;
                IndexCount = Geometry.IndexCount;
                IsFlexible = Geometry.IsFlexible;
                IsTextured = Geometry.IsTextured;
                BoneCount = IsFlexible ? Geometry.Vertices.Max(x => x.BoneWeights.Max(y => y.BoneID)) : 0;
            }
        }

        public bool ReloadModelFromXml()
        {
            var geomElem = GeomtryXml;
            if (geomElem == null)
                geomElem = GetGeometryElementFromProjectFile();

            if (geomElem != null)
            {
                _Geometry = GetGeometryFromElement(geomElem);
                GeometrySaved = _Geometry != null;
            }

            if (GeomtryXml != null)
                GeomtryXml = null;

            UpdateMeshProperties();

            return Geometry != null;
        }

        public XElement GetGeometryElementFromProjectFile()
        {
            var projectXml = Project?.GetProjectXml();
            if (projectXml != null)
            {
                return projectXml.Descendants(NODE_NAME)
                    .FirstOrDefault(e => e.ReadAttribute("ID", string.Empty) == ID);
            }

            return null;
        }

        private MeshGeometry GetGeometryFromElement(XElement element)
        {
            var fakeDoc = new XDocument();

            fakeDoc.Add(new XElement("LddGeometry", element.Nodes().ToArray()));
            fakeDoc.Root.Add(element.Attributes().ToArray());
            return MeshGeometry.FromXml(fakeDoc);
        }

        public void UnloadModel()
        {
            if (GeometrySaved)
                _Geometry = null;
        }

        internal void MarkSaved(bool value)
        {
            GeometrySaved = value;
        }

        public override List<ValidationMessage> ValidateElement()
        {
            var messages = new List<ValidationMessage>();

            void AddMessage(string code, ValidationLevel level, params object[] args)
            {
                messages.Add(new ValidationMessage(this, code, level)
                {
                    MessageArguments = args
                });
            }

            if (IsFlexible)
            {
                bool modelLoaded = IsModelLoaded;
                if (ReloadModelFromXml())
                {
                    var meshBoneIDs = Geometry.Vertices.SelectMany(x => x.BoneWeights.Select(b => b.BoneID)).Distinct();
                    var existingBones = Project.Bones.Select(x => x.BoneID).Distinct();

                    var missingBones = meshBoneIDs.Except(existingBones).ToList();

                    if (missingBones.Any())
                        AddMessage("MESH_MISSING_BONES", ValidationLevel.Error, missingBones);

                    if (!modelLoaded)
                        UnloadModel();
                }
            }

            return messages;
        }
    }
}

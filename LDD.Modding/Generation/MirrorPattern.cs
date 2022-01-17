using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using LDD.Common.Serialization;
using LDD.Common.Simple3D;

namespace LDD.Modding
{
    public class MirrorPattern : ClonePattern
    {
        public override ClonePatternType Type => ClonePatternType.Mirror;

        private Vector3 _Origin;
        private Vector3 _Normal;

        public Vector3 Origin
        {
            get => _Origin;
            set => SetPropertyValue(ref _Origin, value);
        }

        public Vector3 Normal
        {
            get => _Normal;
            set => SetPropertyValue(ref _Normal, value);
        }

        public override XElement SerializeToXml()
        {
            var elem = base.SerializeToXml();
            elem.Add(XmlHelper.ToXmlAttribute(Normal, nameof(Normal)));
            elem.Add(XmlHelper.ToXmlAttribute(Origin, nameof(Origin)));
            return elem;
        }

        protected internal override void LoadFromXml(XElement element)
        {
            base.LoadFromXml(element);

            if (element.HasAttribute(nameof(Normal), out XAttribute axisAttr))
                Normal = XmlHelper.ParseVector3Attribute(axisAttr);

            if (Normal == Vector3.Zero)
                Normal = Vector3.UnitX;

            if (element.HasAttribute(nameof(Origin), out XAttribute originAttr))
                Origin = XmlHelper.ParseVector3Attribute(originAttr);
        }

        public override ItemTransform GetInstanceTransform(Matrix4d baseTransform, ItemTransform transform, int instance)
        {
            if (instance == 0)
                return transform.Clone();

            var origin = baseTransform.TransformPosition(Vector3d.Zero);
            var normal = baseTransform.TransformVector(Vector3d.UnitZ);

            var mirrorMatrix = Matrix4d.Identity;
            mirrorMatrix[2, 2] = -1d;

            var itemTransMat = transform.ToMatrixD();
            var matrixDelta = Matrix4d.GetRelativeMatrix(baseTransform, itemTransMat);

            var itemPos = (Vector3)transform.Position;
            var plane = new Plane((Vector3)origin, (Vector3)normal);

            var ptOnPlane = plane.ProjectPoint(itemPos);
            var dirToPlane = (ptOnPlane - itemPos).Normalized();
            float distance = Vector3.Distance(ptOnPlane, itemPos);
            var finalPt = ptOnPlane + (dirToPlane * distance);
            var tmpMatrix = matrixDelta * baseTransform.Inverted();
            //var upVector = tmpMatrix.TransformVector(Vector3d.UnitY);
            tmpMatrix.ExtractRotation().ToAxisAngle(out Vector3d rotAxis, out double rotAngle);
            rotAxis = mirrorMatrix.TransformVector(rotAxis);
            var finalRot = Quaterniond.FromAxisAngle(rotAxis, -rotAngle);

            //var finalMatrix = baseTransform * tmpMatrix * mirrorMatrix;

            //mirrorMatrix = baseTransform.Inverted() * mirrorMatrix;
            return new ItemTransform((Vector3d)finalPt, finalRot.ToEuler() * (180d / Math.PI));
            //return ItemTransform.FromMatrix(finalMatrix);


            //var up = patternMatrix.TransformVector(Vector3d.UnitY);

            //var finalPt = ptOnPlane + (dirToPlane * distance);

            //var tmpMatrix = Matrix4d.FromQuaternion(patternMatrix.ExtractRotation()) * Matrix4d.FromTranslation((Vector3d)ptOnPlane);

            //var matrixDelta2 = Matrix4d.GetRelativeMatrix(tmpMatrix, itemTransMat);
            //var rotationDelta = matrixDelta2.ExtractRotation().ToEuler();
            //rotationDelta.Y *= -1d;
            //var finalRotation = Quaterniond.FromEuler(rotationDelta) * patternMatrix.ExtractRotation();

            //return new ItemTransform((Vector3d)finalPt, finalRotation.ToEuler() * (180d / Math.PI));

            //var finalMath = matrixDelta2 * Matrix4d.CreateRotationY(Math.PI)  * tmpMatrix;
            //return ItemTransform.FromMatrix(finalMath);


            //var matrixDelta = Matrix4d.GetRelativeMatrix(baseTransMat, itemTransMat);
            //var finalMath = matrixDelta * Matrix4d.FromAngleAxis( Math.PI, Vector3d.UnitZ) * baseTransMat;
            //return ItemTransform.FromMatrix(finalMath);

            //var patternRotation = baseTransMat.ExtractRotation();
            //var itemRotation = transform.ToMatrixD().ExtractRotation();
            //var rotationDelta = itemRotation.Inverted() * patternRotation;
            //var finalRotation = itemRotation * Quaterniond.FromAxisAngle(Vector3d.UnitZ, Math.PI);
            //var rotationAngles = Quaterniond.ToEuler(rotationDelta.Inverted()) * (180d / Math.PI);


            //var ptOnPlane = plane.ProjectPoint(itemPos);
            //var dirToPlane = (ptOnPlane - itemPos).Normalized();
            //float distance = Vector3.Distance(ptOnPlane, itemPos);
            //var finalPt = ptOnPlane + (dirToPlane * distance);


            //return new ItemTransform((Vector3d)finalPt, rotationAngles);
        }

        public override Matrix4d QuantizeTransform(Matrix4d transform)
        {
            var axis = transform.TransformVector(Vector3d.UnitZ);
            var origin = transform.ExtractTranslation();
            var axisMat = Matrix4d.FromDirection(axis, Vector3d.UnitZ);
            var originMat = Matrix4d.FromTranslation(origin);
            return axisMat * originMat;
        }

        public override Matrix4d GetPatternMatrix()
        {
            var axisMat = Matrix4d.FromDirection((Vector3d)Normal, Vector3d.UnitZ);
            var originMat = Matrix4d.FromTranslation((Vector3d)Origin);
            return axisMat * originMat;
        }
    }
}

using System.Dynamic;
using System.Linq;
using System.Xml.Linq;

namespace Common.Utility.Classes.XML
{
    public class DynamicXml : DynamicObject
    {
        private readonly XElement root;

        private DynamicXml(XElement root)
        {
            this.root = root;
        }

        public static DynamicXml Parse(string xmlString)
        {
            return new DynamicXml(RemoveNamespaces(XDocument.Parse(xmlString).Root));
        }

        public static DynamicXml Load(string filename)
        {
            return new DynamicXml(RemoveNamespaces(XDocument.Load(filename).Root));
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = null;

            var att = this.root.Attribute(binder.Name);

            if (att != null)
            {
                result = att.Value;
                return true;
            }

            var nodes = this.root.Elements(binder.Name);

            if (nodes.Count() > 1)
            {
                result = nodes.Select(n => n.HasElements ? (object)new DynamicXml(n) : n.Value).ToList();
                return true;
            }

            var node = this.root.Element(binder.Name);

            if (node != null)
            {
                result = node.HasElements || node.HasAttributes ? new DynamicXml(node) : node.Value;
                return true;
            }

            return true;
        }

        private static XElement RemoveNamespaces(XElement xElem)
        {
            var attrs = xElem.Attributes()
                .Where(a => !a.IsNamespaceDeclaration)
                .Select(a => new XAttribute(a.Name.LocalName, a.Value))
                .ToList();

            if (!xElem.HasElements)
            {
                XElement xElement = new XElement(xElem.Name.LocalName, attrs);
                xElement.Value = xElem.Value;
                return xElement;
            }

            var newXElem = new XElement(xElem.Name.LocalName, xElem.Elements().Select(e => RemoveNamespaces(e)));
            newXElem.Add(attrs);

            return newXElem;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace uk.osric.HtmlLib.Html {
    public interface IElement : INode {

        string NamespaceURI { get; }
        string Prefix { get; }
        string LocalName => NodeName;
        string TagName => NodeName.ToUpperInvariant();

        string Id { get; set; }
        string ClassName { get; set; }

        INamedNodeMap Attributes { get; }

        List<string> GetAttributeNames();

        string? GetAttribute(string qualifiedName);

        void SetAttribute(string qualifiedName, string value);

        void RemoveAttribute(string qualifiedName);

        bool ToggleAttribute(string qualifiedName, bool? force = null);

        bool HasAttribute(string qualifiedName);

        IAttr? GetAttributeNode(string qualifiedName);

        IAttr? SetAttributeNode(IAttr attr);

        IAttr RemoveAttributeNode(IAttr attr);


        /*
        string? GetAttributeNS(string @namespace, string localName);
        void SetAttributeNS(string @namespace, string qualifiedName, string value);
        void RemoveAttributeNS(string @namespace, string localName);
        bool HasAttributeNS(string @namespace, string localName);
        IAttr? SetAttributeNodeNS(IAttr attr);
        IAttr? GetAttributeNodeNS(string @namespace, string localName);
        */

    }
}

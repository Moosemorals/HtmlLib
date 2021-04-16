using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uk.osric.HtmlLib.Html {
    public interface INode {

        public NodeType NodeType { get; }
        public string NodeName { get; }
        public string BaseUri { get; }
        public bool IsConnected { get; }
        public string? NodeValue { get; }
        public string? TextContent { get; }

        public IDocument? OwnerDocument { get; }

        public INode? ParentNode { get; }
        public IElement? ParentElement { get; }

        public INode GetRootNode();
        public bool HasChildNodes();

        public INodeList ChildNodes { get; }
        public INode? FirstChild { get; }
        public INode? LastChild { get; }
        public INode? PreviousSibling { get; }
        public INode? NextSibling { get; }

        public INode InsertBefore(INode node, INode? child);
        public INode AppendChild(INode node);
        public INode ReplaceChild(INode node, INode child);
        public INode RemoveChild(INode child);

        public INode CloneNode(bool deep = false);

        public void Normalize();
        public bool IsEqualNode(INode other);
        public bool IsSameNode(INode other);
        public bool Contains(INode? other);

        public DocumentPosition CompareDocumentPosition(INode other);
    }

    public enum NodeType {
        Element = 1,
        Attribute,
        Text,
        CdataSection,
        EntityReference,
        Entity,
        ProcessingInstruction,
        Comment,
        Document,
        DocumentType,
        DocumentFragment,
        Notation,
    }

    [Flags]
    public enum DocumentPosition {
        Disconnected = 0x01,
        Preceding = 0x02,
        Following = 0x04,
        Contains = 0x08,
        ContainedBy = 0x10,
        ImplementationSpecific = 0x20,
    }


}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uk.osric.HtmlLib.Html {
    internal abstract class Node : INode {
        private Node? _firstChild;
        private Node? _lastChild;
        private Node? _nextSibling;
        private Node? _previousSibling;
        private Node? _parentNode;

        private readonly NodeList _childNodes;

        public Node(string nodeName) {
            NodeName = nodeName;
            _childNodes = new NodeList(this);
        }

        public abstract NodeType NodeType { get; }

        public string NodeName { get; init; }

        public virtual string BaseUri => OwnerDocument?.URL
            ?? ( this as Document )?.URL
            ?? "about:blank";

        public bool IsConnected => OwnerDocument != null;

        public virtual string? NodeValue { get; internal set; }

        public abstract string? TextContent { get; }

        public IDocument? OwnerDocument { get; internal set; }

        public IElement? ParentElement => ParentNode as IElement;

        public INodeList ChildNodes => _childNodes;

        public INode? ParentNode {
            get => _parentNode;
            internal set => _parentNode=  value is Node node ? node : throw new ArgumentException("FirstChild must be Node");
        }

        public INode? FirstChild {
            get => _firstChild;
            internal set => _firstChild =  value is Node node ? node : throw new ArgumentException("FirstChild must be Node");
        }

        public INode? LastChild {
            get => _lastChild;
            internal set => _lastChild =  value is Node node ? node : throw new ArgumentException("LastChild must be Node");
        }

        public INode? PreviousSibling {
            get => _previousSibling;
            internal set => _previousSibling =  value is Node node ? node : throw new ArgumentException("LastChild must be Node");
        }

        public INode? NextSibling {
            get => _nextSibling;
            internal set => _previousSibling =  value is Node node ? node : throw new ArgumentException("LastChild must be Node");
        }

        public INode GetRootNode() {
            return ParentNode == null ? this : ParentNode.GetRootNode();
        }

        public INode AppendChild(INode node) => InsertBefore(node, null);

        public abstract INode CloneNode(bool deep = false);

        public DocumentPosition CompareDocumentPosition(INode other) => throw new NotImplementedException();
        public bool Contains(INode? other) {
            if (other == null) { return false; }

            foreach (Node d in Decendants) {
                if (this == d) {
                    return true;
                }
            }
            return false;
        }

        public bool HasChildNodes() => _firstChild != null;
        public INode InsertBefore(INode node, INode? child) {
            Node? c = child as Node;
            if (node is not Node n) {
                throw new ArgumentException("Node must be a node");
            }

            if (c == n) {
                c = ( node as Node )?._nextSibling;
            }

            if (c == null) {
                if (_lastChild != null) {
                    _lastChild._nextSibling = n;
                    n._previousSibling = _lastChild;
                    n._nextSibling = null;
                    _lastChild = n;
                } else {
                    n._previousSibling = n._nextSibling = null;
                    _firstChild = _lastChild = n;
                }
            } else {
                if (c._parentNode != this) {
                    throw new Exception("Heriachy error");
                }
                n._previousSibling = c._previousSibling;
                n._nextSibling = c;
                c._previousSibling = n;
            }

            n._parentNode = this;

            return n;
        }

        public bool IsEqualNode(INode other) => throw new NotImplementedException();
        public bool IsSameNode(INode other) => throw new NotImplementedException();
        public void Normalize() => throw new NotImplementedException();
        public INode RemoveChild(INode child) {
            if (child is not Node c) { throw new ArgumentException("Child must be node"); }

            if (c.ParentNode != this) { throw new Exception("NotFoundError"); }

            if (c._previousSibling != null) {
                c._previousSibling._nextSibling = c._nextSibling;
            }
            if (c._nextSibling != null) {
                c._nextSibling._previousSibling = c._previousSibling;
            }

            c._parentNode = null;
            c._nextSibling = null;
            c._previousSibling = null;

            return c;
        }
        public INode ReplaceChild(INode node, INode child) => throw new NotImplementedException();

        internal IEnumerable<Node> Decendants {
            get {

                Stack<Node> s = new();
                s.Push(this);
                while (s.Count > 0) {
                    Node next = s.Pop();
                    yield return next;
                    // Push children in reverse so first child is
                    // top of the stack
                    Node? child = next._lastChild;
                    while (child != null) {
                        s.Push(child);
                        child = child._previousSibling;
                    }
                }
                yield break;
            }
        }
    }
}

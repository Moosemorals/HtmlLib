using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uk.osric.HtmlLib.Html {
    public class NodeList : INodeList {

        private readonly INode _source;

        internal NodeList(INode source) {
            _source = source;
        }

        public long Length {
            get {
                long count = 0;

                INode? n = _source.FirstChild;
                while (n != null) {
                    count += 1;
                    n = n.NextSibling;
                }
                return count;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IEnumerator<INode> GetEnumerator() {
            INode? n = _source.FirstChild;
            while (n != null) {
                yield return n;
                n = n.NextSibling;
            }
            yield break;
        }

        public INode? Item(long index) {
            INode? n = _source.FirstChild;
            while (index > 0) {
                if (n == null) {
                    return null;
                }
                index -= 1;
                n = n.NextSibling;
            }
            return n;
        }
    }
}

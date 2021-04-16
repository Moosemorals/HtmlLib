using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uk.osric.HtmlLib {

    public class PrefixTree {
        public class Node {
            public readonly Dictionary<char, Node> Children = new();
            public bool IsLeaf => Value != null;
            public string? Value;

            public Node() {
            }
        }

        public readonly Node Root = new();

        public void Add(string path, string value) {
            Node next = Root;
            int pos = 0;
            while (pos < path.Length) {
                if (!next.Children.ContainsKey(path[pos])) {
                    next.Children.Add(path[pos], new Node());
                }
                next = next.Children[path[pos++]];
            }
            next.Value = value;
        }

        public bool Exists(string word) {
            Node next = Root;
            int pos = 0;
            while (pos < word.Length) {
                if (!next.Children.ContainsKey(word[pos])) {
                    return false;
                }
                next = next.Children[word[pos++]];
            }
            return next.IsLeaf;
        }
    }


}

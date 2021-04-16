﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uk.osric.HtmlLib.Html {
    public interface IText : INode {


        IText SplitText(int offset);
        string WholeText { get; }
    }
}
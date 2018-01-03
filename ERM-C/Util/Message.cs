using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MainForm.Util
{
    public class DetectJobProgressInfo : ICloneable
    {
        protected string mv_message;
        protected bool mv_isEnd;


        public object Clone()
        {
            DetectJobProgressInfo info = new DetectJobProgressInfo();
            info.Message = this.mv_message;
            info.IsEnd = this.IsEnd;
            return info;
        }


        public bool IsEnd
        {
            get { return mv_isEnd; }
            set { mv_isEnd = value; }
        }

        public string Message
        {
            get { return mv_message; }
            set { mv_message = value; }
        }

    }
}

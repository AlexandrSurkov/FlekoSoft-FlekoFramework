﻿using System.Windows.Forms;

namespace Flekosoft.Common.Logging.Windows
{
    public class LoggingListBox : ListBox
    {
        public LoggingListBox()
        {
            //Activate double buffering
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);

            //Enable the OnNotifyMessage event so we get a chance to filter out 
            // Windows messages before they get to the form's WndProc
            SetStyle(ControlStyles.EnableNotifyMessage, true);
        }

        protected override void OnNotifyMessage(Message m)
        {
            // ReSharper disable once CommentTypo
            //Filter out the WM_ERASEBKGND message
            if (m.Msg != 0x14)
            {
                base.OnNotifyMessage(m);
            }
        }


        public bool SuspendRedraw { get; set; }
        protected override void OnPaint(PaintEventArgs e)
        {
            if (!SuspendRedraw) base.OnPaint(e);
        }
    }
}

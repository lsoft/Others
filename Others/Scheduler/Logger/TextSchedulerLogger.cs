using System;

namespace Others.Scheduler.Logger
{
    public class TextSchedulerLogger : ISchedulerLogger
    {
        private readonly Action<string> _writeLineAction;

        public TextSchedulerLogger(
            Action<string> writeLineAction
            )
        {
            if (writeLineAction == null)
            {
                throw new ArgumentNullException("writeLineAction");
            }
            _writeLineAction = writeLineAction;
        }

        public void LogException(Exception excp)
        {
            _writeLineAction("----- EXCEPTION -----");

            if (excp == null)
            {
                _writeLineAction("....is emtpy!");
                return;
            }

            _writeLineAction(excp.Message);
            _writeLineAction(excp.StackTrace);
        }
    }
}
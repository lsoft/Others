using System;

namespace Others.Scheduler.Event
{
    public class SchedulerEventDescription
    {
        public SchedulerEventTypeEnum SchedulerEventType
        {
            get;
            private set;
        }

        public object AdditionalInformation
        {
            get;
            private set;
        }

        public SchedulerEventDescription(
            SchedulerEventTypeEnum schedulerEventType
            )
        {
            SchedulerEventType = schedulerEventType;
            AdditionalInformation = null;
        }

        public SchedulerEventDescription(
            SchedulerEventTypeEnum schedulerEventType,
            object additionalInformation
            )
        {
            if (additionalInformation == null)
            {
                throw new ArgumentNullException("additionalInformation");
            }

            SchedulerEventType = schedulerEventType;
            AdditionalInformation = additionalInformation;
        }
    }
}
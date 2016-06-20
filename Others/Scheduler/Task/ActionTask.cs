using System;

namespace Others.Scheduler.Task
{
    /// <summary>
    /// ����, ������� �������� �������� ����
    /// </summary>
    public class ActionTask : BaseTask
    {
        private readonly Func<bool> _awakeAction;
        private readonly bool _needToRepeatInCaseOfException;

        /// <summary>
        /// ������� ����.
        /// ���� ����� ����������� ��� ����� ������, ���� ������� ���� ��������� �� ��������� ����� 1 ������������.
        /// </summary>
        /// <param name="awakeAction">����, ������� ���������� ��� ������������ �����</param>
        /// <param name="needToRepeatInCaseOfException">� ������ ���������� - ���� �� ��������� ������</param>
        /// <param name="timeoutBetweenAwakes">�������, ����� ������� ������ ��������� �� ������� ������ �������� ���, ���� ������� ��� ���������> �� �������� �������</param>
        public ActionTask(
            Func<bool> awakeAction,
            bool needToRepeatInCaseOfException,
            TimeSpan timeoutBetweenAwakes
            )
            : this(Guid.NewGuid(), awakeAction, needToRepeatInCaseOfException, (long)(timeoutBetweenAwakes.TotalMilliseconds * 1000.0))
        {
        }

        /// <summary>
        /// ������� ����.
        /// ���� ����� ����������� ��� ����� ������, ���� ������� ���� ��������� �� ��������� ����� 1 ������������.
        /// </summary>
        /// <param name="awakeAction">����, ������� ���������� ��� ������������ �����</param>
        /// <param name="needToRepeatInCaseOfException">� ������ ���������� - ���� �� ��������� ������</param>
        /// <param name="microsecondsBetweenAwakes">���������� �����������, ����� ������� ������ ��������� �� ������� ������ �������� ���, ���� ������� ��� ���������> �� �������� �������</param>
        public ActionTask(
            Func<bool> awakeAction,
            bool needToRepeatInCaseOfException,
            long microsecondsBetweenAwakes
            )
            : this(Guid.NewGuid(), awakeAction, needToRepeatInCaseOfException, microsecondsBetweenAwakes)
        {
        }

        /// <summary>
        /// ������� ����.
        /// ���� ����� ����������� ��� ����� ������, ���� ������� ���� ��������� �� ��������� ����� 1 ������������.
        /// </summary>
        /// <param name="taskGuid">���� ������������ �����; ������ - Guid.NewGuid()</param>
        /// <param name="awakeAction">����, ������� ���������� ��� ������������ �����</param>
        /// <param name="needToRepeatInCaseOfException">� ������ ���������� - ���� �� ��������� ������</param>
        /// <param name="timeoutBetweenAwakes">�������, ����� ������� ������ ��������� �� ������� ������ �������� ���, ���� ������� ��� ���������> �� �������� �������</param>
        public ActionTask(
            Guid taskGuid,
            Func<bool> awakeAction,
            bool needToRepeatInCaseOfException,
            TimeSpan timeoutBetweenAwakes
            )
            : base(taskGuid, (long)(timeoutBetweenAwakes.TotalMilliseconds * 1000.0))
        {
            if (awakeAction == null)
            {
                throw new ArgumentNullException("awakeAction");
            }

            _awakeAction = awakeAction;
            _needToRepeatInCaseOfException = needToRepeatInCaseOfException;
        }

        /// <summary>
        /// ������� ����.
        /// ���� ����� ����������� ��� ����� ������, ���� ������� ���� ��������� �� ��������� ����� 1 ������������.
        /// </summary>
        /// <param name="taskGuid">���� ������������ �����; ������ - Guid.NewGuid()</param>
        /// <param name="awakeAction">����, ������� ���������� ��� ������������ �����</param>
        /// <param name="needToRepeatInCaseOfException">� ������ ���������� - ���� �� ��������� ������</param>
        /// <param name="microsecondsBetweenAwakes">���������� �����������, ����� ������� ������ ��������� �� ������� ������ �������� ���, ���� ������� ��� ���������> �� �������� �������</param>
        public ActionTask(
            Guid taskGuid,
            Func<bool> awakeAction,
            bool needToRepeatInCaseOfException,
            long microsecondsBetweenAwakes
            )
            : base(taskGuid, microsecondsBetweenAwakes)
        {
            if (awakeAction == null)
            {
                throw new ArgumentNullException("awakeAction");
            }

            _awakeAction = awakeAction;
            _needToRepeatInCaseOfException = needToRepeatInCaseOfException;
        }

        public override void Execute(
            out bool needToRepeat
            )
        {
            try
            {
                var r = _awakeAction();

                needToRepeat = r;
            }
            catch
            {
                needToRepeat = _needToRepeatInCaseOfException;

                throw;
            }
        }
    }
}
using System;
using Others.Scheduler.Event;
using Others.Scheduler.Task;

namespace Others.Scheduler
{
    /// <summary>
    /// ����������� �����
    /// </summary>
    public interface IScheduler
    {
        /// <summary>
        /// �������, ������������ ���������.
        /// ����������� ���� ������� �� ������ �������� ��������� �����,
        /// ��� ��� ������� ���������� �� ������� ������� ��������.
        /// ��������� ������� - ������ ��������� ������� � ������� ��������� � ���������� ����������,
        /// ����� �� ������� ��� �������, � ������� ��������� � ��������� ������.
        /// </summary>
        event SchedulerEventDelegate SchedulerEvent;

        /// <summary>
        /// ������� ���������� ������ � ��������
        /// </summary>
        int TaskCount
        {
            get;
        }

        /// <summary>
        /// ����� ��������.
        /// </summary>
        void Start(
            );

        /// <summary>
        /// �������� ���� � �������.
        /// </summary>
        void AddTask(
            ITask task
            );

        /// <summary>
        /// �������� ��������� ������.
        /// ���� ������ ��� ������ �����������, �� �� ���������� �� ����� ��������.
        /// </summary>
        /// <param name="taskGuid">���� ������</param>
        void CancelTask(
            Guid taskGuid
            );
    }
}
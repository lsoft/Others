namespace Others.ItemProvider
{
    /// <summary>
    /// Operation result.
    /// ��������� �������� �����.
    /// </summary>
    public enum OperationResultEnum
    {
        /// <summary>
        /// Operation finished successfully.
        /// �������� ��������� �������.
        /// </summary>
        Success,

        /// <summary>
        /// Operation cancelled due to timeout.
        /// �������� �� ��������� ��-�� �������� ��������.
        /// </summary>
        Timeout,

        /// <summary>
        /// Operation cancelled due to dispose is performing.
        /// �������� �� ���������, ��� ��� ����� ������� ������ ���������������.
        /// </summary>
        Dispose,

        /// <summary>
        /// Operation cancelled due to external event.
        /// �������� �������� �� �������� �������
        /// </summary>
        ExternalCondition
    }
}
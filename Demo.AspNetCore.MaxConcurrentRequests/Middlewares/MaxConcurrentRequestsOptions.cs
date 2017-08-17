namespace Demo.AspNetCore.MaxConcurrentRequests.Middlewares
{
    public enum MaxConcurrentRequestsLimitExceededPolicy
    {
        Drop,
        FifoQueueDropTail,
        FifoQueueDropHead
    }

    public class MaxConcurrentRequestsOptions
    {
        #region Constants
        public const int ConcurrentRequestsUnlimited = -1;
        public const int MaxTimeInQueueUnlimited = -1;
        #endregion

        #region Fields
        private int _limit;
        private int _maxQueueLength;
        private int _maxTimeInQueue;
        #endregion

        #region Properties
        public int Limit
        {
            get { return _limit; }

            set { _limit = (value < ConcurrentRequestsUnlimited) ? ConcurrentRequestsUnlimited : value; }
        }

        public MaxConcurrentRequestsLimitExceededPolicy LimitExceededPolicy { get; set; }

        public int MaxQueueLength
        {
            get { return _maxQueueLength; }

            set { _maxQueueLength = (value < 0) ? 0 : value; }
        }

        public int MaxTimeInQueue
        {
            get { return _maxTimeInQueue; }

            set { _maxTimeInQueue = (value <= 0) ? MaxTimeInQueueUnlimited : value; }
        }
        #endregion

        #region Constructor
        public MaxConcurrentRequestsOptions()
        {
            _limit = ConcurrentRequestsUnlimited;
            LimitExceededPolicy = MaxConcurrentRequestsLimitExceededPolicy.Drop;
            _maxQueueLength = 0;
            _maxTimeInQueue = MaxTimeInQueueUnlimited;
        }
        #endregion
    }
}

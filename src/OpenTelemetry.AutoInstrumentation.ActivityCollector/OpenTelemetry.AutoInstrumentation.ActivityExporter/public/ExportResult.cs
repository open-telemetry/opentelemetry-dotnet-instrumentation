using System;

namespace OpenTelemetry.AutoInstrumentation.ActivityExporter
{
    public class ExportResult
    {
        [Flags]
        public enum Status
        {
            /// <summary>
            /// All went well.
            /// </summary>
            Success = 0,

            /// <summary>
            /// Sending failed, but the the specific reason in not clear.
            /// </summary>
            Failure_Unspecified = 64,

            /// <summary>
            /// Could not reach the destination (no network, invalid address, etc..)
            /// </summary>
            Failure_ConnectionError = 128,

            /// <summary>
            /// Submitted payload, byut response timed out.
            /// </summary>
            Failure_ResponseTimeout = 256,

            /// <summary>
            /// Destination returned a general failure code (e.g. 500)
            /// </summary>
            Failure_DestinationFailure = 512,

            /// <summary>
            /// The payload of spans/traces was not acepted by the destination
            /// (payload too large, too small, bad item count, invalid markup or format, etc..)
            /// </summary>
            Failure_InvalidPayload = 1024,

            /// <summary>
            /// At least 1 item, but not all the requested items were successfully exported.
            /// </summary>
            Failure_PartialSuccess = 2048,
        }

        public static ExportResult CreateSuccess(bool isTraceExport, int requestedTraceCount, int requestedActivityCount)
        {
            return CreateSuccess(isTraceExport, requestedTraceCount, requestedActivityCount, nextBatchSizeHint: 0);
        }

        public static ExportResult CreateSuccess(bool isTraceExport, int requestedTraceCount, int requestedActivityCount, int nextBatchSizeHint)
        {
            if (requestedTraceCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(requestedTraceCount));
            }

            if (requestedActivityCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(requestedActivityCount));
            }

            if (isTraceExport == false && requestedTraceCount != 0)
            {
                throw new ArgumentException($"{nameof(isTraceExport)} is false, but {nameof(requestedTraceCount)} is not zero ({requestedTraceCount}).");
            }

            if (nextBatchSizeHint < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(nextBatchSizeHint));
            }

            return new ExportResult(
                        isTraceExport: isTraceExport,
                        requestedTraceCount: requestedTraceCount,
                        requestedActivityCount: requestedActivityCount,
                        statusCode: Status.Success,
                        error: null,
                        errorMessage: null,
                        nextBatchSizeHint: nextBatchSizeHint);
        }

        public static ExportResult CreateFailure(
                                            bool isTraceExport,
                                            int requestedTraceCount,
                                            int requestedActivityCount,
                                            Status statusCode,
                                            Exception error)
        {
            return CreateFailure(isTraceExport, requestedTraceCount, requestedActivityCount, statusCode, error, errorMessage: null, nextBatchSizeHint: 0);
        }

        public static ExportResult CreateFailure(
                                            bool isTraceExport,
                                            int requestedTraceCount,
                                            int requestedActivityCount,
                                            Status statusCode,
                                            Exception error,
                                            int nextBatchSizeHint)
        {
            return CreateFailure(isTraceExport, requestedTraceCount, requestedActivityCount, statusCode, error, errorMessage: null, nextBatchSizeHint);
        }

        public static ExportResult CreateFailure(
                                            bool isTraceExport,
                                            int requestedTraceCount,
                                            int requestedActivityCount,
                                            Status statusCode,
                                            string errorMessage)
        {
            return CreateFailure(isTraceExport, requestedTraceCount, requestedActivityCount, statusCode, error: null, errorMessage, nextBatchSizeHint: 0);
        }

        public static ExportResult CreateFailure(
                                            bool isTraceExport,
                                            int requestedTraceCount,
                                            int requestedActivityCount,
                                            Status statusCode,
                                            string errorMessage,
                                            int nextBatchSizeHint)
        {
            return CreateFailure(isTraceExport, requestedTraceCount, requestedActivityCount, statusCode, error: null, errorMessage, nextBatchSizeHint);
        }

        public static ExportResult CreateFailure(
                                            bool isTraceExport, 
                                            int requestedTraceCount, 
                                            int requestedActivityCount, 
                                            Status statusCode, 
                                            Exception error, 
                                            string errorMessage, 
                                            int nextBatchSizeHint)
        {
            if (requestedTraceCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(requestedTraceCount));
            }

            if (requestedActivityCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(requestedActivityCount));
            }

            if (isTraceExport == false && requestedTraceCount != 0)
            {
                throw new ArgumentException($"{nameof(isTraceExport)} is false, but {nameof(requestedTraceCount)} is not zero ({requestedTraceCount}).");
            }

            if (statusCode != Status.Failure_Unspecified 
                    && statusCode != Status.Failure_ConnectionError
                    && statusCode != Status.Failure_ResponseTimeout
                    && statusCode != Status.Failure_DestinationFailure
                    && statusCode != Status.Failure_InvalidPayload
                    && statusCode != Status.Failure_PartialSuccess)
            {
                throw new ArgumentException($"{nameof(statusCode)} must be a Failure status code, but the specified value is \"{statusCode.ToString()}\" ({((int) statusCode)}).");
            }

            if (error == null && errorMessage == null)
            {
                throw new ArgumentException($"{nameof(error)} and {nameof(errorMessage)} cannot both be null; at least one (or both) of these values must be specified.");
            }

            if (nextBatchSizeHint < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(nextBatchSizeHint));
            }

            return new ExportResult(
                        isTraceExport: isTraceExport,
                        requestedTraceCount: requestedTraceCount,
                        requestedActivityCount: requestedActivityCount,
                        statusCode: statusCode,
                        error: error,
                        errorMessage: errorMessage,
                        nextBatchSizeHint: nextBatchSizeHint);
        }

        private ExportResult(
                        bool isTraceExport, 
                        int requestedTraceCount, 
                        int requestedActivityCount, 
                        ExportResult.Status statusCode, 
                        Exception error, 
                        string errorMessage, 
                        int nextBatchSizeHint)
        {
            this.IsTraceExport = isTraceExport;
            this.RequestedTraceCount = requestedTraceCount;
            this.RequestedActivityCount = requestedActivityCount;
            this.StatusCode = statusCode;
            this.Error = error;
            this.ErrorMessage = errorMessage;
            this.NextBatchSizeHint = nextBatchSizeHint;
        }

        public bool IsTraceExport { get; }
        public bool IsActivityExport { get { return !IsTraceExport; } }
        public int RequestedTraceCount { get; }
        public int RequestedActivityCount { get; }
        public bool IsSuccess { get { return (StatusCode == Status.Success); } }

        public Status StatusCode { get; private set; }

        public Exception Error { get; private set; }

        public string ErrorMessage { get; private set; }

        public int NextBatchSizeHint { get; private set; }
    }
}

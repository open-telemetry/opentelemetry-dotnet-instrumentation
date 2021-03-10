namespace Datadog.Trace
{
    /// <summary>
    /// Span execution status.
    /// </summary>
    public readonly struct SpanStatus : System.IEquatable<SpanStatus>
    {
        /// <summary>
        /// The operation completed successfully.
        /// </summary>
        public static readonly SpanStatus Ok = new SpanStatus(StatusCode.Ok);

        /// <summary>
        /// The default status.
        /// </summary>
        public static readonly SpanStatus Unset = new SpanStatus(StatusCode.Unset);

        /// <summary>
        /// The operation contains an error.
        /// </summary>
        public static readonly SpanStatus Error = new SpanStatus(StatusCode.Error);

        internal SpanStatus(StatusCode statusCode, string description = null)
        {
            this.StatusCode = statusCode;
            this.Description = description;
        }

        /// <summary>
        /// Gets the canonical code from this status.
        /// </summary>
        public StatusCode StatusCode { get; }

        /// <summary>
        /// Gets the status description.
        /// </summary>
        /// <remarks>
        /// Note: Status Description is only valid for <see
        /// cref="StatusCode.Error"/> Status and will be ignored for all other
        /// <see cref="Trace.StatusCode"/> values. See the <a
        /// href="https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/trace/api.md#set-status">
        /// Status API</a> for details.
        /// </remarks>
        public string Description { get; }

        /// <summary>
        /// Compare two <see cref="SpanStatus"/> for equality.
        /// </summary>
        /// <param name="status1">First Status to compare.</param>
        /// <param name="status2">Second Status to compare.</param>
        public static bool operator ==(SpanStatus status1, SpanStatus status2) => status1.Equals(status2);

        /// <summary>
        /// Compare two <see cref="SpanStatus"/> for not equality.
        /// </summary>
        /// <param name="status1">First Status to compare.</param>
        /// <param name="status2">Second Status to compare.</param>
        public static bool operator !=(SpanStatus status1, SpanStatus status2) => !status1.Equals(status2);

        /// <summary>
        /// Returns a new instance of a status with the description populated.
        /// </summary>
        /// <remarks>
        /// Note: Status Description is only valid for <see
        /// cref="StatusCode.Error"/> Status and will be ignored for all other
        /// <see cref="Trace.StatusCode"/> values. See the <a
        /// href="https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/trace/api.md#set-status">Status
        /// API</a> for details.
        /// </remarks>
        /// <param name="description">Description of the status.</param>
        /// <returns>New instance of the status class with the description populated.</returns>
        public SpanStatus WithDescription(string description)
        {
            if (this.StatusCode != StatusCode.Error || this.Description == description)
            {
                return this;
            }

            return new SpanStatus(this.StatusCode, description);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (!(obj is SpanStatus))
            {
                return false;
            }

            var that = (SpanStatus)obj;
            return this.StatusCode == that.StatusCode && this.Description == that.Description;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var result = 1;
            result = (31 * result) + this.StatusCode.GetHashCode();
            result = (31 * result) + (this.Description?.GetHashCode() ?? 0);
            return result;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return nameof(SpanStatus)
                + "{"
                + nameof(this.StatusCode) + "=" + this.StatusCode + ", "
                + nameof(this.Description) + "=" + this.Description
                + "}";
        }

        /// <inheritdoc/>
        public bool Equals(SpanStatus other)
        {
            return this.StatusCode == other.StatusCode && this.Description == other.Description;
        }
    }
}

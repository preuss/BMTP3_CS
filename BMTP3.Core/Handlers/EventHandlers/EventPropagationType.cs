namespace BMTP3.Core.Handlers.EventHandlers {
	/// <summary>
	/// Specifies how an event should propagate after being handled.
	/// </summary>
	public enum EventPropagationType {
		/// <summary>
		/// Boolean value should be false.
		/// This is Undhandled event or stopped event.
		/// Indicates that the event should be propagated further.
		/// Continue propagating the event to other handlers.
		/// This means the event has not been handled here and will be handled by the next handler.
		/// </summary>
		ContinuePropagation = 0,

		/// <summary>
		/// Boolean value should be true.
		/// This is handled event or cancelled event.
		/// Indicates that the event should not be propagated further.
		/// Stop propagating the event and handle it here.
		/// This means the event has been handled here and will not be handled by other handlers.
		/// </summary>
		StopPropagation = 1,
	}
}

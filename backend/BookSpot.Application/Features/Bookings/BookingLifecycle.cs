namespace BookSpot.Application.Features.Bookings;

public static class BookingLifecycle
{
    public static bool TryTransition(string source, string action, string actorRole, out string target)
    {
        target = (source, action, actorRole) switch
        {
            ("pending", "confirm", "provider") => "confirmed",
            ("pending", "decline", "provider") => "declined",
            ("pending" or "confirmed", "cancel", "client" or "provider") => "cancelled",
            ("confirmed", "complete", "provider") => "completed",
            ("confirmed", "mark_no_show", "provider") => "no_show",
            ("pending" or "confirmed", "reschedule", "client" or "provider") => "pending",
            _ => string.Empty
        };

        return target.Length > 0;
    }
}

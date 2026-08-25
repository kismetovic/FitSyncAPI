using FITSync.Domain.Entities;
using FITSync.Domain.Enums;

namespace FITSync.Infrastructure.Notifications;

/// <summary>
/// One place that decides what a user is told, keyed by what actually happened.
/// This is what stops a reservation that is still waiting for trainer approval from
/// being announced as "confirmed": each status has its own wording.
/// </summary>
public static class NotificationTemplates
{
    public record Message(string Title, string Body, string EmailSubject, string EmailHtml);

    private static string Wrap(string heading, string bodyHtml) => $@"
        <h2>{heading}</h2>
        {bodyHtml}
        <p>Srdačan pozdrav,<br/>FitSync tim</p>";

    private static string Slot(Reservation reservation)
        => reservation.ReservationDate.ToString("dd.MM.yyyy HH:mm");

    private static string TrainingName(Reservation reservation)
        => reservation.Training?.Name ?? "trening";

    /// <summary>
    /// Reservation was created. The wording depends on whether it still needs approval -
    /// a PendingApproval request must never read like a confirmation.
    /// </summary>
    public static Message ReservationCreated(Reservation reservation)
    {
        var training = TrainingName(reservation);
        var slot = Slot(reservation);

        if (reservation.Status == ReservationStatus.PendingApproval)
        {
            return new Message(
                "Zahtjev za rezervaciju zaprimljen",
                $"Vaš zahtjev za trening \"{training}\" u terminu {slot} je zaprimljen i čeka odobrenje trenera.",
                "Zahtjev za rezervaciju zaprimljen - FitSync",
                Wrap("Zahtjev zaprimljen", $@"
                    <p>Vaš zahtjev za rezervaciju je zaprimljen i <strong>čeka odobrenje trenera</strong>.</p>
                    <ul>
                        <li><strong>Trening:</strong> {training}</li>
                        <li><strong>Termin:</strong> {slot}</li>
                        <li><strong>Iznos:</strong> {reservation.TotalPrice:0.00} BAM</li>
                    </ul>
                    <p>Obavijestit ćemo Vas čim trener odgovori na zahtjev. Rezervacija još nije potvrđena.</p>"));
        }

        return new Message(
            "Rezervacija kreirana",
            $"Rezervacija za trening \"{training}\" u terminu {slot} je kreirana. Preostalo je izvršiti uplatu.",
            "Rezervacija kreirana - FitSync",
            Wrap("Rezervacija kreirana", $@"
                <p>Vaša rezervacija je kreirana.</p>
                <ul>
                    <li><strong>Trening:</strong> {training}</li>
                    <li><strong>Termin:</strong> {slot}</li>
                    <li><strong>Iznos za uplatu:</strong> {reservation.TotalPrice:0.00} BAM</li>
                </ul>
                <p>Rezervacija se smatra potvrđenom nakon evidentirane uplate.</p>"));
    }

    public static Message ReservationApproved(Reservation reservation)
    {
        var training = TrainingName(reservation);
        var slot = Slot(reservation);
        return new Message(
            "Rezervacija odobrena",
            $"Vaša rezervacija za trening \"{training}\" u terminu {slot} je odobrena.",
            "Rezervacija odobrena - FitSync",
            Wrap("Rezervacija odobrena", $@"
                <p>Trener je odobrio Vašu rezervaciju.</p>
                <ul>
                    <li><strong>Trening:</strong> {training}</li>
                    <li><strong>Termin:</strong> {slot}</li>
                    <li><strong>Iznos za uplatu:</strong> {reservation.TotalPrice:0.00} BAM</li>
                </ul>"));
    }

    public static Message ReservationPaid(Reservation reservation, decimal amount, string currency)
    {
        var training = TrainingName(reservation);
        var slot = Slot(reservation);
        return new Message(
            "Uplata evidentirana",
            $"Uplata od {amount:0.00} {currency} za trening \"{training}\" ({slot}) je evidentirana. Rezervacija je potvrđena.",
            "Uplata evidentirana - FitSync",
            Wrap("Uplata evidentirana", $@"
                <p>Zaprimili smo Vašu uplatu i rezervacija je potvrđena.</p>
                <ul>
                    <li><strong>Trening:</strong> {training}</li>
                    <li><strong>Termin:</strong> {slot}</li>
                    <li><strong>Uplaćeno:</strong> {amount:0.00} {currency}</li>
                </ul>"));
    }

    public static Message ReservationCancelled(Reservation reservation, string reason, bool cancelledByStaff)
    {
        var training = TrainingName(reservation);
        var slot = Slot(reservation);
        var who = cancelledByStaff ? "Osoblje teretane je otkazalo" : "Otkazali ste";
        return new Message(
            "Rezervacija otkazana",
            $"{who} rezervaciju za trening \"{training}\" u terminu {slot}. Razlog: {reason}",
            "Rezervacija otkazana - FitSync",
            Wrap("Rezervacija otkazana", $@"
                <p>{who} rezervaciju.</p>
                <ul>
                    <li><strong>Trening:</strong> {training}</li>
                    <li><strong>Termin:</strong> {slot}</li>
                    <li><strong>Razlog:</strong> {reason}</li>
                </ul>"));
    }

    /// <summary>Sent to the trainer/administrator side when a client cancels.</summary>
    public static Message ReservationCancelledStaffCopy(Reservation reservation, string reason, string clientName)
    {
        var training = TrainingName(reservation);
        var slot = Slot(reservation);
        return new Message(
            "Klijent je otkazao rezervaciju",
            $"{clientName} je otkazao/la rezervaciju za trening \"{training}\" u terminu {slot}. Razlog: {reason}",
            "Klijent je otkazao rezervaciju - FitSync",
            Wrap("Klijent je otkazao rezervaciju", $@"
                <ul>
                    <li><strong>Klijent:</strong> {clientName}</li>
                    <li><strong>Trening:</strong> {training}</li>
                    <li><strong>Termin:</strong> {slot}</li>
                    <li><strong>Razlog:</strong> {reason}</li>
                </ul>"));
    }

    /// <summary>Sent to staff when a client requests a slot outside the trainer's hours.</summary>
    public static Message OutsideAvailabilityRequested(Reservation reservation, string clientName)
    {
        var training = TrainingName(reservation);
        var slot = Slot(reservation);
        return new Message(
            "Zahtjev van radnog vremena trenera",
            $"{clientName} traži termin {slot} za trening \"{training}\" izvan dostupnosti trenera. Zahtjev čeka odobrenje.",
            "Zahtjev van radnog vremena - FitSync",
            Wrap("Zahtjev van radnog vremena trenera", $@"
                <ul>
                    <li><strong>Klijent:</strong> {clientName}</li>
                    <li><strong>Trening:</strong> {training}</li>
                    <li><strong>Termin:</strong> {slot}</li>
                    <li><strong>Doplata:</strong> {reservation.OutsideAvailabilitySurcharge:0.00} BAM</li>
                </ul>"));
    }

    public static Message ReservationCompleted(Reservation reservation)
    {
        var training = TrainingName(reservation);
        return new Message(
            "Trening završen",
            $"Trening \"{training}\" je evidentiran kao završen. Ostavite recenziju i pomozite drugim korisnicima.",
            "Trening završen - FitSync",
            Wrap("Trening završen", $@"
                <p>Nadamo se da Vam se trening ""{training}"" svidio.</p>
                <p>Sada možete ostaviti recenziju u aplikaciji.</p>"));
    }

    public static Message PaymentReminder(IReadOnlyCollection<string> unpaidDetails)
    {
        var list = string.Join("; ", unpaidDetails);
        var count = unpaidDetails.Count;
        var noun = count == 1 ? "neplaćenu rezervaciju" : "neplaćene rezervacije";
        return new Message(
            "Podsjetnik za uplatu",
            $"Imate {count} {noun}: {list}",
            "Podsjetnik za uplatu - FitSync",
            Wrap("Podsjetnik za uplatu", $@"
                <p>Evidentirali smo da uplata još nije izvršena za:</p>
                <p>{list}</p>
                <p>Molimo Vas da uplatu izvršite u najkraćem roku.</p>"));
    }

    public static Message MembershipPurchased(UserMembership membership)
    {
        var name = membership.MembershipPackage?.Name ?? "mjesečni paket";
        return new Message(
            "Paket čeka uplatu",
            $"Paket \"{name}\" je rezervisan i čeka uplatu: {membership.SessionsTotal} termina, važi do {membership.EndDate:dd.MM.yyyy}.",
            "Paket čeka uplatu - FitSync",
            Wrap("Paket čeka uplatu", $@"
                <ul>
                    <li><strong>Paket:</strong> {name}</li>
                    <li><strong>Broj termina:</strong> {membership.SessionsTotal}</li>
                    <li><strong>Važi od:</strong> {membership.StartDate:dd.MM.yyyy}</li>
                    <li><strong>Važi do:</strong> {membership.EndDate:dd.MM.yyyy}</li>
                </ul>"));
    }

    /// <summary>
    /// Sent when a package is actually paid for. Distinct from MembershipPurchased,
    /// which now only means the package was reserved and is waiting for payment.
    /// </summary>
    public static Message MembershipPaid(UserMembership membership, decimal amount, string currency)
    {
        var name = membership.MembershipPackage?.Name ?? "mjesečni paket";
        var money = $"{amount:0.00} {currency}";
        return new Message(
            "Paket plaćen i aktiviran",
            $"Paket \"{name}\" je plaćen ({money}) i aktiviran: {membership.SessionsTotal} termina, važi do {membership.EndDate:dd.MM.yyyy}.",
            "Paket plaćen i aktiviran - FitSync",
            Wrap("Paket plaćen i aktiviran", $@"
                <ul>
                    <li><strong>Paket:</strong> {name}</li>
                    <li><strong>Plaćeno:</strong> {money}</li>
                    <li><strong>Broj termina:</strong> {membership.SessionsTotal}</li>
                    <li><strong>Važi od:</strong> {membership.StartDate:dd.MM.yyyy}</li>
                    <li><strong>Važi do:</strong> {membership.EndDate:dd.MM.yyyy}</li>
                </ul>"));
    }

    public static Message Welcome(string userName) => new(
        "Dobrodošli u FitSync",
        $"Zdravo {userName}, Vaš nalog je kreiran. Pregledajte treninge i rezervišite svoj termin.",
        "Dobrodošli u FitSync",
        Wrap($"Dobrodošli, {userName}!", "<p>Hvala što ste se registrovali. Sada možete pregledati treninge i kreirati rezervacije.</p>"));
}

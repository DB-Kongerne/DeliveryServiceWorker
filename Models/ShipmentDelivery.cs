namespace DeliveryServiceWorker.Models {
public class ShipmentDelivery
{
    public string MedlemsNavn { get; set; }
    public string PickupAdresse { get; set; }
    public string PakkeId { get; set; }
    public string AfleveringsAdresse { get; set; }

    public ShipmentDelivery(string medlemsNavn, string pickupAdresse, string pakkeId, string afleveringsAdresse)
    {
        MedlemsNavn = medlemsNavn;
        PickupAdresse = pickupAdresse;
        PakkeId = pakkeId;
        AfleveringsAdresse = afleveringsAdresse;
    }
}
}
namespace InventoryHold.Domain;

public class InventoryHoldDomainException : Exception
{
    public InventoryHoldDomainException(string message) : base(message) { }
}

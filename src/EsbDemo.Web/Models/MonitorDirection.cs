namespace EsbDemo.Web.Models;

/// <summary>
/// The kind of row in the bus monitor's message log.
/// </summary>
public enum MonitorDirection
{
    /// <summary>
    /// The app published the message to the bus.
    /// </summary>
    Send,



    /// <summary>
    /// The bus delivered the message to the app.
    /// </summary>
    Receive,



    /// <summary>
    /// An expected response did not arrive before the timeout.
    /// </summary>
    Timeout,
}

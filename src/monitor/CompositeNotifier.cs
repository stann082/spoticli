using core.monitor;
using Serilog;

namespace monitor;

/// <summary>
/// Fans a result out to every configured channel. Each is isolated, so a broken SMTP server still
/// leaves you with the toast (and vice versa).
/// </summary>
public class CompositeNotifier : INotifier
{

    #region Constructors

    public CompositeNotifier(IEnumerable<INotifier> notifiers)
    {
        _notifiers = notifiers.ToArray();
    }

    #endregion

    #region Variables

    private readonly INotifier[] _notifiers;

    #endregion

    #region Public Methods

    public async Task NotifyAsync(MonitorRunResult result, CancellationToken cancellationToken = default)
    {
        foreach (var notifier in _notifiers)
        {
            try
            {
                await notifier.NotifyAsync(result, cancellationToken);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{Notifier} failed; continuing with the remaining channels", notifier.GetType().Name);
            }
        }
    }

    #endregion

}

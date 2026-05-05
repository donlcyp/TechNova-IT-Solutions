using Serilog.Core;
using Serilog.Events;

namespace TechNova_IT_Solutions.Infrastructure
{
    /// <summary>
    /// Serilog enricher that filters sensitive data from log messages.
    /// This enricher adds a property to indicate when sensitive data has been detected.
    /// The actual filtering happens in the SensitiveDataFilter class.
    /// </summary>
    public class SensitiveDataEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            // Check if the message template contains sensitive data
            if (logEvent.MessageTemplate != null)
            {
                var originalMessage = logEvent.MessageTemplate.Text;
                if (SensitiveDataFilter.ContainsSensitiveData(originalMessage))
                {
                    logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                        "SensitiveDataDetected", true));
                }
            }

            // Check exception messages for sensitive data
            if (logEvent.Exception != null)
            {
                var originalExceptionMessage = logEvent.Exception.Message;
                if (SensitiveDataFilter.ContainsSensitiveData(originalExceptionMessage))
                {
                    var filteredExceptionMessage = SensitiveDataFilter.FilterSensitiveData(originalExceptionMessage);
                    logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                        "FilteredExceptionMessage", filteredExceptionMessage));
                }
            }

            // Check properties for sensitive data
            foreach (var property in logEvent.Properties)
            {
                if (property.Value is ScalarValue scalarValue && scalarValue.Value is string stringValue)
                {
                    if (SensitiveDataFilter.ContainsSensitiveData(stringValue))
                    {
                        var filteredValue = SensitiveDataFilter.FilterSensitiveData(stringValue);
                        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                            $"Filtered_{property.Key}", filteredValue));
                    }
                }
            }
        }
    }
}

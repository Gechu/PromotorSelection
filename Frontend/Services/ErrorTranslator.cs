using System.Net;
using System.Text.Json;

namespace PromotorSelection.Services
{
    public static class ErrorTranslator
    {
        public static string Translate(HttpResponseMessage response)
        {
            var details = ReadResponseBody(response);

            return response.StatusCode switch
            {
                HttpStatusCode.Unauthorized => "Email lub hasło są niepoprawne.",
                HttpStatusCode.NotFound => "Nie znaleziono szukanego elementu.",
                HttpStatusCode.Conflict => TranslateConflict(details),
                HttpStatusCode.BadRequest => TranslateBadRequest(details),
                HttpStatusCode.InternalServerError => "Coś poszło nie tak. Spróbuj ponownie za chwilę.",
                _ when (int)response.StatusCode >= 500 => "Coś poszło nie tak. Spróbuj ponownie za chwilę.",
                _ => !string.IsNullOrWhiteSpace(details)
                    ? details
                    : "Nie udało się wykonać operacji. Spróbuj ponownie."
            };
        }

        private static string TranslateBadRequest(string? details)
        {
            if (!string.IsNullOrWhiteSpace(details) && !IsTechnical(details))
            {
                return details;
            }

            return "Wprowadzone dane są niepoprawne. Sprawdź formularz i spróbuj ponownie.";
        }

        private static string TranslateConflict(string? details)
        {
            if (!string.IsNullOrWhiteSpace(details))
            {
                if (details.Contains("album", StringComparison.OrdinalIgnoreCase))
                {
                    return "Numer albumu już istnieje w systemie.";
                }

                if (details.Contains("email", StringComparison.OrdinalIgnoreCase))
                {
                    return "Email już istnieje w systemie.";
                }

                if (!IsTechnical(details))
                {
                    return details;
                }
            }

            return "Email już istnieje w systemie.";
        }

        private static string ReadResponseBody(HttpResponseMessage response)
        {
            try
            {
                var raw = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                return ExtractMessage(raw) ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string? ExtractMessage(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            var trimmed = raw.Trim();
            if (!trimmed.StartsWith("{") && !trimmed.StartsWith("["))
            {
                return trimmed;
            }

            try
            {
                using var doc = JsonDocument.Parse(trimmed);
                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (TryGetString(root, "error", out var error))
                    {
                        return error;
                    }

                    if (TryGetString(root, "message", out var message))
                    {
                        return message;
                    }

                    if (TryGetString(root, "title", out var title))
                    {
                        return title;
                    }

                    if (root.TryGetProperty("errors", out var errorsNode))
                    {
                        var errors = ExtractErrors(errorsNode);
                        if (!string.IsNullOrWhiteSpace(errors))
                        {
                            return errors;
                        }
                    }
                }
            }
            catch
            {
                return trimmed;
            }

            return trimmed;
        }

        private static string? ExtractErrors(JsonElement node)
        {
            if (node.ValueKind == JsonValueKind.Array)
            {
                var list = new List<string>();
                foreach (var item in node.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        var value = item.GetString();
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            list.Add(value!);
                        }
                        continue;
                    }

                    if (item.ValueKind == JsonValueKind.Object)
                    {
                        if (TryGetString(item, "ErrorMessage", out var errorMessage))
                        {
                            list.Add(errorMessage);
                            continue;
                        }

                        if (TryGetString(item, "message", out var message))
                        {
                            list.Add(message);
                        }
                    }
                }

                return list.Count > 0 ? string.Join(" ", list.Distinct()) : null;
            }

            if (node.ValueKind == JsonValueKind.Object)
            {
                var list = new List<string>();
                foreach (var property in node.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in property.Value.EnumerateArray())
                        {
                            if (item.ValueKind == JsonValueKind.String)
                            {
                                var value = item.GetString();
                                if (!string.IsNullOrWhiteSpace(value))
                                {
                                    list.Add(value!);
                                }
                            }
                        }
                    }
                }

                return list.Count > 0 ? string.Join(" ", list.Distinct()) : null;
            }

            return null;
        }

        private static bool TryGetString(JsonElement root, string propertyName, out string value)
        {
            value = string.Empty;
            if (!root.TryGetProperty(propertyName, out var element) || element.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            var text = element.GetString();
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            value = text!;
            return true;
        }

        private static bool IsTechnical(string text)
        {
            return text.Contains("BadRequest", StringComparison.OrdinalIgnoreCase)
                || text.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase)
                || text.Contains("NotFound", StringComparison.OrdinalIgnoreCase)
                || text.Contains("Conflict", StringComparison.OrdinalIgnoreCase)
                || text.Contains("Internal Server Error", StringComparison.OrdinalIgnoreCase)
                || text.Contains("HTTP", StringComparison.OrdinalIgnoreCase)
                || text.Contains("Exception", StringComparison.OrdinalIgnoreCase);
        }
    }
}

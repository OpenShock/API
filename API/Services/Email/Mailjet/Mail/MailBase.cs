﻿using OpenShock.API.Utils;
using System.Text.Json.Serialization;

namespace OpenShock.API.Services.Email.Mailjet.Mail;

[JsonConverter(typeof(OneWayPolymorphicJsonConverter<MailBase>))]
public abstract class MailBase
{
    public required Contact From { get; set; }
    public required IEnumerable<Contact> To { get; set; }
    public required string Subject { get; set; }
    public Dictionary<string, string>? Variables { get; set; }
}
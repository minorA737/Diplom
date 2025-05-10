// Models/SystemSettings.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ManufactPlanner.Models
{
    [Table("system_settings")]
    public class SystemSettings
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("smtp_server")]
        public string SmtpServer { get; set; } = "smtp.gmail.com";

        [Column("smtp_port")]
        public int SmtpPort { get; set; } = 587;

        [Column("smtp_username")]
        public string SmtpUsername { get; set; } = "manufact.planner@gmail.com";

        [Column("smtp_password")]
        public string SmtpPassword { get; set; } = "";

        [Column("smtp_use_ssl")]
        public bool SmtpUseSsl { get; set; } = true;

        [Column("email_sender_name")]
        public string EmailSenderName { get; set; } = "ManufactPlanner";

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
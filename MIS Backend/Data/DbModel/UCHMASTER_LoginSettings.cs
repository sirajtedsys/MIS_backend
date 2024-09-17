using System.ComponentModel.DataAnnotations;

namespace MIS_Backend.Data.DbModel
{
    public class UCHMASTER_LoginSettings
    {
        [Key]
        public int ID { get; set; }
        public int USERID { get; set; }
        public string TOKEN { get; set; }
        public DateTime GENERATEDATE { get; set; }

    }
}

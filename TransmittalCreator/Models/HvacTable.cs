namespace TransmittalCreator.Models
{
    public class HvacTable
    {
        /// <summary>
        /// Номер комнаты
        /// </summary>
        public string RoomNumber { get; set; }

        /// <summary>
        /// название комнаты
        /// </summary>
        public string RoomName { get; set; }

        /// <summary>
        /// отопление
        /// </summary>
        public string Heating { get; set; }

        /// <summary>
        /// охлаждение
        /// </summary>

        public string Cooling { get; set; }

        /// <summary>
        /// приточка
        /// </summary>
        public string AirExchangeSupply{ get; set; }

        /// <summary>
        /// вытяжка
        /// </summary>
        public string AirExchangeExhaust { get; set; }

        public HvacTable(string roomNumber, string roomName, string heating, string cooling, string airExchangeSupply, string airExchangeExhaust)
        {
            RoomNumber = roomNumber;
            RoomName = roomName;
            Heating = heating;
            Cooling = cooling;
            AirExchangeSupply = airExchangeSupply;
            AirExchangeExhaust = airExchangeExhaust;
        }
        
        
    }
}
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
        /// температура комнаты
        /// </summary>
        public string RoomTemp { get; set; }

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
        public string AirExchangeSupplyInd{ get; set; }


        /// <summary>
        /// вытяжка
        /// </summary>
        public string AirExchangeExhaust { get; set; }
        public string AirExchangeExhaustInd { get; set; }

        public HvacTable(string roomNumber, string roomName, string roomTemp, string heating, string cooling, string airExchangeSupply, string supplyInd, string airExchangeExhaust, string exhaustInd)
        {
            RoomNumber = roomNumber;
            RoomName = roomName;
            RoomTemp = roomTemp;
            Heating = heating;
            Cooling = cooling;
            AirExchangeSupply = airExchangeSupply;
            AirExchangeSupplyInd = supplyInd;
            AirExchangeExhaust = airExchangeExhaust;
            AirExchangeExhaustInd = exhaustInd;
        }
        
        
    }
}
namespace TransmittalCreator.Models
{
    public class HvacTable
    {
        private string supply;
        private string supplyInd;
        private string exhaust;
        private string exhaustInd;

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

        public HvacTable(string roomNumber, string roomName, string heating, string cooling, string supply, string supplyInd, string exhaust, string exhaustInd)
        {
            RoomNumber = roomNumber;
            RoomName = roomName;
            Heating = heating;
            Cooling = cooling;
            this.supply = supply;
            this.supplyInd = supplyInd;
            this.exhaust = exhaust;
            this.exhaustInd = exhaustInd;
        }
    }
}
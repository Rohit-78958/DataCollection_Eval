using System;
using System.Collections.Generic;

namespace DataCollection_Eval
{

	public class MachineInfoDTO
	{
		public string IpAddress {  get; set; }

		public int PortNo { get; set; }

		public string MachineId { get; set; }

		public string DataCollectionProtocol { get; set; }

		public List<GaugeInformation> Gauges { get; set; }

	}

	public class PLCAddressInfo
	{
		public string ProfinetAddrString { get; set; }
		public string OutputDatatype { get; set; }
		public int ProfinetStrtAddr { get; set; }
		public int DBNumber { get; set; }
		public int PointsToRead { get; set; }
		public int AckAddress { get; set; }
		public int CommunicationAddress { get; set; }
		public ushort DateNStatusAddr { get; set; } = ushort.MinValue;
		public ushort DateNStatusAckAddr { get; set; } = ushort.MinValue;
	}

	public class GaugeInformation
	{
        public string MachineID { get; set; }
        public string GaugeID { get; set; }
        public string DataSource { get; set; }
        public List<DimensionDetails> DimensionDetails { get; set; }
		public int DimensionsCount { get; set; }
    }

    public class DimensionDetails
    {
        public string DimensionID { get; set; }
        public double MeasuredValue { get; set; }
    }
}
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
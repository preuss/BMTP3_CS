﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3_CS.Metadata {
	internal interface IMetadataFileInfo {
		public DateTime? GetCreatedMediaFileDateTime();
		public FileInfo GetSourceFileInfo();
	}
}

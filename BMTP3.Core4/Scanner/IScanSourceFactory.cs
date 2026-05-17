namespace BMTP3.Core4.Scanner;

internal interface IScanSourceFactory
{
	IScanSource Create(ScanSourceCreateRequest request);
}

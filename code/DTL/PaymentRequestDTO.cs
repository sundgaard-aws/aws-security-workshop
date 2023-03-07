using System;
using Amazon.DynamoDBv2.DataModel;

namespace OM.AWS.Demo.DTL
{
    [DynamoDBTable("aws-sec-pay-req")] public class PaymentRequestDTO
    {
        public enum StatusEnum { CREATED, SENT_TO_EXTERNAL_PP, CONFIRMED }
        [DynamoDBHashKey] public DateTime? PaymentDate { get; set; }
        [DynamoDBRangeKey] public string? PaymentsFileGUID { get; set; }
        public StatusEnum Status { get; set; }
    }
}
using System;
using Amazon.DynamoDBv2.DataModel;

namespace OM.AWS.Demo.DTL
{
    [DynamoDBTable("aws-sec-payment")] public class PaymentDTO
    {
        public enum StatusEnum { CREATED, SENT_TO_EXTERNAL_PP, CONFIRMED }
        [DynamoDBHashKey] public DateTime? PaymentDate { get; set; }
        [DynamoDBRangeKey] public string? PaymentGUID { get; set; }
        public StatusEnum Status { get; set; }
    }
}
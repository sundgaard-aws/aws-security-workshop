import * as Core from '@aws-cdk/core';
import { Effect, IRole, PolicyStatement, ServicePrincipal } from "@aws-cdk/aws-iam";
import { AttributeType, BillingMode, Table } from '@aws-cdk/aws-dynamodb';
import { MetaData } from './meta-data';
import { Bucket, EventType, IBucket } from '@aws-cdk/aws-s3';
import { RemovalPolicy, StackProps, Tags } from '@aws-cdk/core';
import { Alias, IKey, Key } from '@aws-cdk/aws-kms';
import { v4 } from 'uuid';
import { IQueue, Queue, QueueEncryption } from '@aws-cdk/aws-sqs';
import { SqsDestination } from '@aws-cdk/aws-s3-notifications';
import * as SSM from '@aws-cdk/aws-ssm';
import { SSMHelper } from './ssm-helper';


export interface DataStackProps extends StackProps {
    key: IKey;
    enableGrants: boolean;
    //sqsRequestEventTarget: IQueue;
    //sqsResponseEventTarget: IQueue;
}

export class DataStack extends Core.Stack {
    private ssmHelper = new SSMHelper();
    private apiRole:IRole;
    private keyAlias: Alias | undefined;
    private props:DataStackProps;
    private PaymentRequestBucket:IBucket;
    private PaymentResponseBucket:IBucket;
    private PaymentRequestQueue: {queue:IQueue,dlq:IQueue};
    private PaymentResponseQueue: {queue:IQueue,dlq:IQueue};
    private PaymentInputBucket: IBucket;
    
    constructor(scope: Core.Construct, id: string, apiRole: IRole, props?: DataStackProps) {
        super(scope, id, props);
        this.apiRole = apiRole;
        if(props==undefined) throw("Please make sure that the properties are initialized!");
        this.props=props;
        this.keyAlias=props?.key.addAlias(v4().toString());
        this.createPaymentRequestTable();
        this.PaymentRequestQueue=this.createPaymentRequestSQSQueue();
        this.PaymentResponseQueue=this.createPaymentResponseSQSQueue();
        this.PaymentInputBucket=this.createPaymentInputBucket();
        this.PaymentRequestBucket=this.createRequestBucket();
        this.PaymentResponseBucket=this.createResponseBucket();
        this.addExtraPermissions(props.key);
        this.addEvents();
        //props.key.grantEncryptDecrypt(this.PaymentRequestBucket.bucketArn);        
    }

    private addExtraPermissions(key:IKey) {
        // QUEUE Policy
        /*
        {
            "Version": "2012-10-17",
            "Id": "example-ID",
            "Statement": [
                {
                    "Sid": "example-statement-ID",
                    "Effect": "Allow",
                    "Principal": {
                        "Service": "s3.amazonaws.com"
                    },
                    "Action": [
                        "SQS:SendMessage"
                    ],
                    "Resource": "arn:aws:sqs:Region:account-id:queue-name",
                    "Condition": {
                        "ArnLike": {
                            "aws:SourceArn": "arn:aws:s3:*:*:awsexamplebucket1"
                        },
                        "StringEquals": {
                            "aws:SourceAccount": "bucket-owner-account-id"
                        }
                    }
                }
            ]
        }*/
        this.PaymentRequestQueue.queue.addToResourcePolicy(new PolicyStatement({
            effect: Effect.ALLOW,
            principals: [new ServicePrincipal('s3.amazonaws.com')],
            actions: ["SQS:SendMessage"],
            resources: [this.PaymentRequestQueue.queue.queueArn]
        }));
        this.PaymentRequestQueue.dlq.addToResourcePolicy(new PolicyStatement({
            effect: Effect.ALLOW,
            principals: [new ServicePrincipal('s3.amazonaws.com')],
            actions: ["SQS:SendMessage"],
            resources: [this.PaymentRequestQueue.dlq.queueArn]
        }));
        this.PaymentResponseQueue.queue.addToResourcePolicy(new PolicyStatement({
            effect: Effect.ALLOW,
            principals: [new ServicePrincipal('s3.amazonaws.com')],
            actions: ["SQS:SendMessage"],
            resources: [this.PaymentResponseQueue.queue.queueArn]
        }));
        this.PaymentResponseQueue.dlq.addToResourcePolicy(new PolicyStatement({
            effect: Effect.ALLOW,
            principals: [new ServicePrincipal('s3.amazonaws.com')],
            actions: ["SQS:SendMessage"],
            resources: [this.PaymentResponseQueue.dlq.queueArn]
        })); 

        // KEY POLICY
        /*{
            "Version": "2012-10-17",
            "Id": "example-ID",
            "Statement": [
                {
                    "Sid": "example-statement-ID",
                    "Effect": "Allow",
                    "Principal": {
                        "Service": "s3.amazonaws.com"
                    },
                    "Action": [
                        "kms:GenerateDataKey",
                        "kms:Decrypt"
                    ],
                    "Resource": "*"
                }
            ]
        }*/
        key.addToResourcePolicy(new PolicyStatement({
            effect: Effect.ALLOW,
            principals: [new ServicePrincipal('s3.amazonaws.com'), new ServicePrincipal('sqs.amazonaws.com')],
            actions: ["kms:GenerateDataKey","kms:Decrypt"],
            resources: ["*"]
        }));        
    }

    private createPaymentInputBucket():IBucket {
        var name = MetaData.PREFIX+"pay-inp";
        var bucket = new Bucket(this, name, {
            bucketName: name, 
            encryptionKey: this.keyAlias,
            removalPolicy: RemovalPolicy.DESTROY
        });
        bucket.grantReadWrite(this.apiRole);
        var ssmParam=this.ssmHelper.createSSMParameter(this, MetaData.PREFIX+"PaymentInputBucketName", name, SSM.ParameterType.STRING);
        ssmParam.grantRead(this.apiRole);
        //bucket.addEventNotification(EventType.OBJECT_CREATED, new SqsDestination(this.props.sqsRequestEventTarget));
        Core.Tags.of(bucket).add(MetaData.NAME, name);
        return bucket;
    }

    private createRequestBucket():IBucket {
        var name = MetaData.PREFIX+"pay-req";
        var bucket = new Bucket(this, name, {
            bucketName: name, 
            encryptionKey: this.keyAlias,
            removalPolicy: RemovalPolicy.DESTROY
        });
        this.ssmHelper.createSSMParameter(this, MetaData.PREFIX+"PaymentRequestBucketName", name, SSM.ParameterType.STRING);
        bucket.grantReadWrite(this.apiRole);
        //bucket.addEventNotification(EventType.OBJECT_CREATED, new SqsDestination(this.props.sqsRequestEventTarget));
        Core.Tags.of(bucket).add(MetaData.NAME, name);
        return bucket;
    }    
    
    private createResponseBucket():IBucket {
        var name = MetaData.PREFIX+"pay-res";
        var bucket = new Bucket(this, name, {
            bucketName: name, encryptionKey: this.keyAlias,
            removalPolicy: RemovalPolicy.DESTROY
        });
        this.ssmHelper.createSSMParameter(this, MetaData.PREFIX+"PaymentResponseBucketName", name, SSM.ParameterType.STRING);
        bucket.grantReadWrite(this.apiRole);
        //bucket.addEventNotification(EventType.OBJECT_CREATED, new SqsDestination(this.props.sqsRequestEventTarget));
        Core.Tags.of(bucket).add(MetaData.NAME, name);
        return bucket;
    }    
    
    private createPaymentRequestTable() {
        var name = MetaData.PREFIX+"pay-req-tab";
        new Table(this, name, {
            tableName: name,
            billingMode: BillingMode.PAY_PER_REQUEST,
            partitionKey: {name: "PaymentDate", type: AttributeType.STRING},
            sortKey: {name: "PaymentsFileGUID", type: AttributeType.STRING}
        });
    }

    private createPaymentRequestSQSQueue():{queue:IQueue,dlq:IQueue}
    {
        var queue=this.createSQSQueue("payment-request");
        // https://docs.aws.amazon.com/AmazonS3/latest/userguide/grant-destinations-permissions-to-s3.html
        //queue.grantSendMessages(new ServicePrincipal('s3.amazonaws.com'));
        return queue;
    }

    private createPaymentResponseSQSQueue():{queue:IQueue,dlq:IQueue}
    {
        var queue=this.createSQSQueue("payment-response");
        //queue.grantSendMessages(new ServicePrincipal('s3.amazonaws.com'));
        return queue;
    }

    private createSQSQueue(prefix:string):{queue:IQueue,dlq:IQueue}
    {
        var dlqName=MetaData.PREFIX+prefix+"-dlq-sqs";
        var qName=MetaData.PREFIX+prefix+"-sqs";
        var ssmQueueURLParameterName=MetaData.PREFIX+prefix+"-sqs-queue-url";
        var deadLetterQueue = new Queue(this, dlqName, {
            queueName: dlqName, visibilityTimeout: Core.Duration.seconds(4), retentionPeriod: Core.Duration.days(14), removalPolicy: RemovalPolicy.DESTROY
        });
        Core.Tags.of(deadLetterQueue).add(MetaData.NAME, dlqName);
        
        var queue = new Queue(this, qName, {
            queueName: qName, visibilityTimeout: Core.Duration.seconds(4), retentionPeriod: Core.Duration.days(14), 
            removalPolicy: RemovalPolicy.DESTROY,
            deadLetterQueue: {queue: deadLetterQueue, maxReceiveCount: 5}, 
            encryption: QueueEncryption.KMS, encryptionMasterKey:this.keyAlias
            //,fifo: false
        });
        Core.Tags.of(queue).add(MetaData.NAME, qName);
        this.ssmHelper.createSSMParameter(this, ssmQueueURLParameterName, queue.queueUrl, SSM.ParameterType.STRING);
        return {queue:queue,dlq:deadLetterQueue};
    }    

    private addEvents() {
        //this.PaymentRequestBucket.addEventNotification(EventType.OBJECT_CREATED_PUT, new SqsDestination(this.PaymentRequestQueue.queue));
        //this.PaymentResponseBucket.addEventNotification(EventType.OBJECT_CREATED, new SqsDestination(this.PaymentResponseQueue));
        //Core.Tags.of(bucket).add(MetaData.NAME, name);
    }
}
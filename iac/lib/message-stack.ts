import * as Core from '@aws-cdk/core';
import { MetaData } from './meta-data';
import { IQueue, Queue, QueueEncryption } from '@aws-cdk/aws-sqs';
import { SSMHelper } from './ssm-helper';
import * as SSM from '@aws-cdk/aws-ssm';
import { IKey } from '@aws-cdk/aws-kms';
import { StackProps } from '@aws-cdk/core';
import { v4 } from 'uuid';

export interface MessageStackProps extends StackProps {
    key: IKey;
}

export class MessageStack extends Core.Stack {
    private ssmHelper = new SSMHelper();
    public PaymentRequestQueue: IQueue;
    public PaymentResponseQueue: IQueue;
    private keyAlias: IKey | undefined;
    constructor(scope: Core.Construct, id: string, props?: MessageStackProps) {
        super(scope, id, props);
        this.keyAlias=props?.key.addAlias(v4().toString());
        
    }


}
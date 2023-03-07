import { expect as expectCDK, matchTemplate, MatchStyle } from '@aws-cdk/assert';
import { Capture, Match, Template } from "@aws-cdk/assertions";
import { CfnVPC, Vpc } from '@aws-cdk/aws-ec2';
import { CfnSubnet } from '@aws-cdk/aws-ec2/lib/ec2.generated';
import * as cdk from '@aws-cdk/core';
import * as NS from '../lib/network-stack';

test('Network Stack Test', () => {
    const app = new cdk.App();
    const networkStack = new NS.NetworkStack(app, 'NetworkStackTest', {
    });

    const networkTemplate = Template.fromStack(networkStack);
    var res=networkTemplate.findResources(CfnVPC.CFN_RESOURCE_TYPE_NAME);
    //console.log(res);
    networkTemplate.hasResourceProperties(CfnVPC.CFN_RESOURCE_TYPE_NAME, {
      "CidrBlock": "10.10.0.0/16",
      "EnableDnsHostnames": true,
      "EnableDnsSupport": true
    });
    res=networkTemplate.findResources(CfnSubnet.CFN_RESOURCE_TYPE_NAME);
    //console.log(res);
    networkTemplate.resourceCountIs(CfnSubnet.CFN_RESOURCE_TYPE_NAME, 4);
});


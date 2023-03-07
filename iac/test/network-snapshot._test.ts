import { expect as expectCDK, matchTemplate, MatchStyle, expect } from '@aws-cdk/assert';
import { Capture, Match, Template } from "@aws-cdk/assertions";
import { CfnVPC, Vpc } from '@aws-cdk/aws-ec2';
import { CfnSubnet } from '@aws-cdk/aws-ec2/lib/ec2.generated';
import * as cdk from '@aws-cdk/core';
import * as NS from '../lib/network-stack';
import * as FS from 'fs';
import { Stack } from '@aws-cdk/core';

test('Network Stack Snapshot Test', () => {
    const app = new cdk.App();
    const networkStack = new NS.NetworkStack(app, 'NetworkStackTest', {
    });

    const networkTemplate = Template.fromStack(networkStack);
    //expectCDK(networkStack).toMatch()
    //expect(networkTemplate.toJSON()).toMatchSnapshot();
    
    //FS.writeFileSync("test.here", "abc");
    var jsonString=FS.readFileSync('test/snapshots/aws-sec-network-stack.template.json', 'utf-8');
    var jsonObj=JSON.parse(jsonString);
    //expect(networkStack).toMatch(Template.fromJSON(jsonObj));
    expect(networkStack).toMatch(jsonObj);
});


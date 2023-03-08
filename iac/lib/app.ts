#!/usr/bin/env node
import 'source-map-support/register';
import * as cdk from '@aws-cdk/core';
import { env } from 'process';
import { MetaData } from './meta-data';
import { NetworkStack } from './network-stack';
import { ComputeStack } from './compute-stack';
import { DataStack } from './data-stack';
import { SecurityStack } from './security-stack';

const app = new cdk.App();
var region=process.env["CDK_DEFAULT_REGION"];
region="eu-north-1";
var enableGrants=true
var props = {env: {account: process.env["CDK_DEFAULT_ACCOUNT"], region: region }, enableGrants:enableGrants };
var metaData = new MetaData();

var networkStack = new NetworkStack(app, MetaData.PREFIX+"network-stack", props);
var securityStack = new SecurityStack(app, MetaData.PREFIX+"security-stack", networkStack.Vpc, props);
var computeStack = new ComputeStack(app, MetaData.PREFIX+"compute-stack", networkStack.Vpc, securityStack.ApiSecurityGroup, securityStack.ApiRole, securityStack.cmk, props);
var dataStack=new DataStack(app, MetaData.PREFIX+"data-stack", securityStack.ApiRole, { 
    env: {account: process.env["CDK_DEFAULT_ACCOUNT"], region: region }, 
    key:securityStack.cmk,
    enableGrants:enableGrants
});

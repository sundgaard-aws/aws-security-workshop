import * as Core from '@aws-cdk/core';
import EC2 = require('@aws-cdk/aws-ec2');
import { MetaData } from './meta-data';
import { InterfaceVpcEndpointAwsService, Port, VpcEndpointService } from '@aws-cdk/aws-ec2';
import { IVpc, SubnetType } from '@aws-cdk/aws-ec2/lib/vpc';
import { ISecurityGroup, SecurityGroup } from '@aws-cdk/aws-ec2/lib/security-group';
import { Effect, IRole, ManagedPolicy, PolicyStatement, Role, ServicePrincipal } from '@aws-cdk/aws-iam';
import { IKey, Key } from '@aws-cdk/aws-kms';
import { SSMHelper } from './ssm-helper';
import * as SSM from '@aws-cdk/aws-ssm';
import { Secret } from '@aws-cdk/aws-secretsmanager';
import { RemovalPolicy, RemoveTag } from '@aws-cdk/core';

export class NetworkStack extends Core.Stack {
    public Vpc:EC2.IVpc;    
    public SSMVPCEndpointSG: ISecurityGroup;
    public cmk:IKey;
    public ApiSecurityGroup: ISecurityGroup;
    public ApiRole:IRole;
    private ssmHelper = new SSMHelper();
    private SecretsManagerVPCEndpointSG: EC2.ISecurityGroup;

    constructor(scope: Core.Construct, id: string, props?: Core.StackProps) {
        super(scope, id, props);
        this.Vpc = this.createVPC();
        this.SSMVPCEndpointSG=this.createSSMVPCEndpointSecurityGroup();
        this.SecretsManagerVPCEndpointSG=this.createSecretsManagerVPCEndpointSecurityGroup();
        this.createEndpoints(this.Vpc);
        this.cmk=this.createCustomerManagedKey();        
        //if(MetaData.EnableGrants) this.cmk.grantEncryptDecrypt(new ServicePrincipal('s3.amazonaws.com'));
        //if(MetaData.EnableGrants) this.cmk.grantEncryptDecrypt(new ServicePrincipal('sqs.amazonaws.com'));
        this.ApiSecurityGroup = this.createAPISecurityGroup(this.Vpc);
        this.SSMVPCEndpointSG.addIngressRule(this.ApiSecurityGroup, Port.allTraffic(), "Allow APIs to call SSM");
        this.ApiRole = this.buildAPIRole();
        this.createCryptoSecrets();
    }

    private createCryptoSecrets() {
        var secretName=MetaData.PREFIX+"crypto-secret";
        const cryptoContextSecret = new Secret(this, secretName, {
            generateSecretString: {
              secretStringTemplate: JSON.stringify({ UserName: "<replace>", PassPhrase:"<replace>" }),
              generateStringKey: "PassPhrase"
            },
            secretName:secretName
          });
        var ssmParam=this.ssmHelper.createSSMParameter(this, MetaData.PREFIX+"CryptoSecretName", secretName, SSM.ParameterType.STRING);
        ssmParam.grantRead(this.ApiRole);
        cryptoContextSecret.grantRead(this.ApiRole);

        var materialSecretName=MetaData.PREFIX+"crypto-mat-secret";
        const cryptoKeyMaterialSecret = new Secret(this, materialSecretName, {
            generateSecretString: {
              secretStringTemplate: JSON.stringify({ PublicKey: "<replace>", PrivateKey:"<replace>" }),
              generateStringKey: "PrivateKey"
            },
            secretName:materialSecretName
          });
        ssmParam=this.ssmHelper.createSSMParameter(this, MetaData.PREFIX+"CryptoMaterialSecretName", materialSecretName, SSM.ParameterType.STRING);
        ssmParam.grantRead(this.ApiRole);
        cryptoKeyMaterialSecret.grantRead(this.ApiRole);
        cryptoKeyMaterialSecret.grantWrite(this.ApiRole);
    }
    
    private createEndpoints(vpc: EC2.IVpc) {
        vpc.addGatewayEndpoint(MetaData.PREFIX+"dyndb-ep", {
            service: EC2.GatewayVpcEndpointAwsService.DYNAMODB,
            subnets: [
                 { subnetType: EC2.SubnetType.PRIVATE_WITH_NAT }, { subnetType: EC2.SubnetType.PUBLIC }
            ]
        });
        vpc.addGatewayEndpoint(MetaData.PREFIX+"s3-ep", {
            service: EC2.GatewayVpcEndpointAwsService.S3,
            subnets: [
                 { subnetType: EC2.SubnetType.PRIVATE_WITH_NAT }, { subnetType: EC2.SubnetType.PUBLIC }
            ]
        });
        vpc.addInterfaceEndpoint(MetaData.PREFIX+"ssm-ep", {

            service: InterfaceVpcEndpointAwsService.SSM,
            subnets: vpc.selectSubnets({subnetType:SubnetType.PRIVATE_WITH_NAT}),
            securityGroups: [this.SSMVPCEndpointSG]
        });
        vpc.addInterfaceEndpoint(MetaData.PREFIX+"sm-ep", {

            service: InterfaceVpcEndpointAwsService.SECRETS_MANAGER,
            subnets: vpc.selectSubnets({subnetType:SubnetType.PRIVATE_WITH_NAT}),
            securityGroups: [this.SecretsManagerVPCEndpointSG]
        });

    }

    private createCustomerManagedKey(): IKey {
        var name=MetaData.PREFIX+"key";
        var key=new Key(this, name, {
            description: name,
            enabled: true            
        });
        Core.Tags.of(key).add(MetaData.NAME, name);
        return key;
    }

    private createAPISecurityGroup(vpc: IVpc): ISecurityGroup {
        var postFix = "api-sg";
        var securityGroup = new SecurityGroup(this, MetaData.PREFIX+postFix, {
            vpc: vpc,
            securityGroupName: MetaData.PREFIX+postFix,
            description: MetaData.PREFIX+postFix,
            allowAllOutbound: true
        });
        
        //securityGroup.connections.allowTo(this.metaData.RDSSecurityGroup, EC2.Port.tcp(3306), "Lambda to RDS");
        Core.Tags.of(securityGroup).add(MetaData.NAME, MetaData.PREFIX+postFix);
        //this.metaData.APISecurityGroup = securityGroup;
        return securityGroup;
    }

    private buildAPIRole(): IRole {
        var role = new Role(this, MetaData.PREFIX+"api-role", {
            description: "Lambda API Role",
            roleName: MetaData.PREFIX+"api-role",
            assumedBy: new ServicePrincipal("lambda.amazonaws.com"),
            managedPolicies: [
                ManagedPolicy.fromAwsManagedPolicyName("AWSStepFunctionsFullAccess"),
                ManagedPolicy.fromAwsManagedPolicyName("AmazonSSMFullAccess"),
                ManagedPolicy.fromManagedPolicyArn(this, "AWSLambdaSQSQueueExecutionRole", "arn:aws:iam::aws:policy/service-role/AWSLambdaSQSQueueExecutionRole"),
                ManagedPolicy.fromManagedPolicyArn(this, "AWSLambdaBasicExecutionRole", "arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole"),
                ManagedPolicy.fromManagedPolicyArn(this, "AWSLambdaVPCAccessExecutionRole", "arn:aws:iam::aws:policy/service-role/AWSLambdaVPCAccessExecutionRole")
            ],
        });
        role.addToPolicy(new PolicyStatement({
          effect: Effect.ALLOW,
          resources: ["*"],
          actions: ["secretsmanager:GetSecretValue","dbqms:*","rds-data:*","xray:*","dynamodb:GetItem","dynamodb:PutItem","dynamodb:UpdateItem","dynamodb:Scan","dynamodb:Query"]
        }));

        Core.Tags.of(role).add(MetaData.NAME, MetaData.PREFIX+"api-role");
        return role;
    }     

    private createSSMVPCEndpointSecurityGroup(): ISecurityGroup {
        var sg=new SecurityGroup(this, MetaData.PREFIX+"ssm-vpce-sg", {
            securityGroupName: MetaData.PREFIX+"ssm-vpce-sg",
            disableInlineRules: false,
            allowAllOutbound: true,
            description: "",
            vpc: this.Vpc
        });
        return sg;
    }

    private createSecretsManagerVPCEndpointSecurityGroup(): ISecurityGroup {
        var sg=new SecurityGroup(this, MetaData.PREFIX+"sm-vpce-sg", {
            securityGroupName: MetaData.PREFIX+"sm-vpce-sg",
            disableInlineRules: false,
            allowAllOutbound: true,
            description: "Secrets Manager VPC Endpoint Security Group",
            vpc: this.Vpc
        });
        sg.applyRemovalPolicy(RemovalPolicy.DESTROY);
        return sg;        
    }
    
    private createVPC():EC2.IVpc {
        // Link: https://blog.codecentric.de/en/2019/09/aws-cdk-create-custom-vpc/
        var vpc = new EC2.Vpc(this, MetaData.PREFIX+"vpc", {
            cidr: "10.10.0.0/16", subnetConfiguration: [
                { cidrMask: 24, name: MetaData.PREFIX+"private-sne", subnetType: EC2.SubnetType.PRIVATE_WITH_NAT },
                { cidrMask: 25, name: MetaData.PREFIX+"public-sne", subnetType: EC2.SubnetType.PUBLIC }
            ],
            natGateways: 1,
            maxAzs: 2
        });
        
        var publicNacl = this.createPublicNacl(vpc);
        vpc.publicSubnets.forEach( subnet => { subnet.associateNetworkAcl(MetaData.PREFIX+"public-nacl-assoc", publicNacl) } );
        var privateNacl = this.createPrivateNacl(vpc);
        vpc.privateSubnets.forEach( subnet => { subnet.associateNetworkAcl(MetaData.PREFIX+"private-nacl-assoc", privateNacl) } );
        
        this.tagVPCResources(vpc);
        
        return vpc;
    }
    
    private createPublicNacl(vpc: EC2.Vpc):EC2.INetworkAcl {
        var publicNacl = new EC2.NetworkAcl(this, MetaData.PREFIX+"public-nacl", {
            vpc: vpc,
            networkAclName: MetaData.PREFIX+"public-nacl",
            subnetSelection: {
                subnetType: EC2.SubnetType.PUBLIC
            }
        });
        publicNacl.addEntry(MetaData.PREFIX+"public-nacl-allow-all-inbound", {
           cidr: EC2.AclCidr.anyIpv4(),
           direction: EC2.TrafficDirection.INGRESS,
           ruleAction: EC2.Action.ALLOW,
           ruleNumber: 500,
           traffic: EC2.AclTraffic.allTraffic(),
           networkAclEntryName: "all-traffic"
        });
        publicNacl.addEntry(MetaData.PREFIX+"public-nacl-allow-all-outbound", {
           cidr: EC2.AclCidr.anyIpv4(),
           direction: EC2.TrafficDirection.EGRESS,
           ruleAction: EC2.Action.ALLOW,
           ruleNumber: 500,
           traffic: EC2.AclTraffic.allTraffic(),
           networkAclEntryName: "all-traffic"
        });        
        Core.Tags.of(publicNacl).add(MetaData.NAME, MetaData.PREFIX+"public-nacl");
        return publicNacl;
    }
    
    private createPrivateNacl(vpc: EC2.Vpc):EC2.INetworkAcl {
        var privateNacl = new EC2.NetworkAcl(this, MetaData.PREFIX+"private-nacl", {
            vpc: vpc,
            networkAclName: MetaData.PREFIX+"private-nacl",
            subnetSelection: {
                subnetType: EC2.SubnetType.PRIVATE_WITH_NAT
            }
        });
        privateNacl.addEntry(MetaData.PREFIX+"private-nacl-allow-all-inbound", {
           cidr: EC2.AclCidr.anyIpv4(),
           direction: EC2.TrafficDirection.INGRESS,
           ruleAction: EC2.Action.ALLOW,
           ruleNumber: 500,
           traffic: EC2.AclTraffic.allTraffic(),
           networkAclEntryName: "all-traffic"
        });
        privateNacl.addEntry(MetaData.PREFIX+"private-nacl-deny-inbound-ssh", {
           cidr: EC2.AclCidr.anyIpv4(),
           direction: EC2.TrafficDirection.INGRESS,
           ruleAction: EC2.Action.DENY,
           ruleNumber: 100,
           traffic: EC2.AclTraffic.tcpPort(22),
           networkAclEntryName: "deny-ssh"
        });        
        privateNacl.addEntry(MetaData.PREFIX+"private-nacl-allow-all-outbound", {
           cidr: EC2.AclCidr.anyIpv4(),
           direction: EC2.TrafficDirection.EGRESS,
           ruleAction: EC2.Action.ALLOW,
           ruleNumber: 500,
           traffic: EC2.AclTraffic.allTraffic(),
           networkAclEntryName: "all-traffic"
        });
        Core.Tags.of(privateNacl).add(MetaData.NAME, MetaData.PREFIX+"private-nacl");
        return privateNacl;
    }     
    
    private tagVPCResources(vpc: EC2.Vpc) {
        Core.Tags.of(vpc).add(MetaData.NAME, MetaData.PREFIX+"vpc");
        Core.Tags.of(vpc).add(MetaData.NAME, MetaData.PREFIX+"igw", { includeResourceTypes: [EC2.CfnInternetGateway.CFN_RESOURCE_TYPE_NAME] });
        //Core.Tags.of(vpc).add(MetaData.NAME, MetaData.PREFIX+"nat", { includeResourceTypes: [EC2.CfnNatGateway.CFN_RESOURCE_TYPE_NAME]});
        Core.Tags.of(vpc).add(MetaData.NAME, MetaData.PREFIX+"default-nacl", { includeResourceTypes: [EC2.CfnNetworkAcl.CFN_RESOURCE_TYPE_NAME]});
        var defaultNacl = EC2.NetworkAcl.fromNetworkAclId(vpc, MetaData.PREFIX+"vpc", vpc.vpcDefaultNetworkAcl);
        Core.Tags.of(defaultNacl).add(MetaData.NAME, MetaData.PREFIX+"default-nacl");
        
        Core.Tags.of(vpc).add(MetaData.NAME, MetaData.PREFIX+"default-sg", { includeResourceTypes: [EC2.CfnSecurityGroup.CFN_RESOURCE_TYPE_NAME]});
        
        vpc.publicSubnets.forEach( subnet => {
            Core.Tags.of(subnet).add(MetaData.NAME, MetaData.PREFIX+"public-sne", { includeResourceTypes: [EC2.CfnSubnet.CFN_RESOURCE_TYPE_NAME]});
            Core.Tags.of(subnet).add(MetaData.NAME, MetaData.PREFIX+"public-rt", { includeResourceTypes: [EC2.CfnRouteTable.CFN_RESOURCE_TYPE_NAME]});
            Core.Tags.of(subnet).add(MetaData.NAME, MetaData.PREFIX+"public-nacl", { includeResourceTypes: [EC2.CfnNetworkAcl.CFN_RESOURCE_TYPE_NAME]});
        });
        
        vpc.privateSubnets.forEach( subnet => {
            Core.Tags.of(subnet).add(MetaData.NAME, MetaData.PREFIX+"private-sne", { includeResourceTypes: [EC2.CfnSubnet.CFN_RESOURCE_TYPE_NAME]});
            Core.Tags.of(subnet).add(MetaData.NAME, MetaData.PREFIX+"private-rt", { includeResourceTypes: [EC2.CfnRouteTable.CFN_RESOURCE_TYPE_NAME]});
            Core.Tags.of(subnet).add(MetaData.NAME, MetaData.PREFIX+"private-nacl", { includeResourceTypes: [EC2.CfnNetworkAcl.CFN_RESOURCE_TYPE_NAME]});
        });
        
        vpc.isolatedSubnets.forEach( subnet => {
            Core.Tags.of(subnet).add(MetaData.NAME, MetaData.PREFIX+"isolated-sne", { includeResourceTypes: [EC2.CfnSubnet.CFN_RESOURCE_TYPE_NAME]});
            Core.Tags.of(subnet).add(MetaData.NAME, MetaData.PREFIX+"isolated-rt", { includeResourceTypes: [EC2.CfnRouteTable.CFN_RESOURCE_TYPE_NAME]});
            Core.Tags.of(subnet).add(MetaData.NAME, MetaData.PREFIX+"isolated-nacl", { includeResourceTypes: [EC2.CfnNetworkAcl.CFN_RESOURCE_TYPE_NAME]});
        });
    }
}
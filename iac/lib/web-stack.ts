import { Distribution, OriginAccessIdentity } from '@aws-cdk/aws-cloudfront';
import { S3Origin } from '@aws-cdk/aws-cloudfront-origins';
import { BlockPublicAccess, Bucket, IBucket } from '@aws-cdk/aws-s3';
import { BucketDeployment, Source } from '@aws-cdk/aws-s3-deployment';
import * as Core from '@aws-cdk/core';
import { MetaData } from './meta-data';
import { SSMHelper } from './ssm-helper';

export class WebStack extends Core.Stack {
    private ssmHelper = new SSMHelper();

    constructor(scope: Core.Construct, id: string, props?: Core.StackProps) {
        super(scope, id, props);        
        var staticResourcesBucket=this.createStaticResourcesBucket();
        this.createDistribution(staticResourcesBucket);        
    }

    private createStaticResourcesBucket() : IBucket {
        var name = MetaData.PREFIX+"static-web";
        var bucket = new Bucket(this, name, {
            //websiteIndexDocument: "index.html",
            publicReadAccess: false,
            blockPublicAccess: BlockPublicAccess.BLOCK_ALL,
            bucketName: name
        });        
        new BucketDeployment(this, MetaData.PREFIX+"web-dep", {
            sources: [Source.asset("../src/static")],
            destinationBucket: bucket
        });

        Core.Tags.of(bucket).add(MetaData.NAME, name);
        return bucket;
    }

    private createDistribution(staticResourcesBucket: IBucket) {
        var name = MetaData.PREFIX+"cf-dist";
        //var oai=new OriginAccessIdentity(this, MetaData.PREFIX+"cf-oai");
        //var s3Origin=new S3Origin(staticResourcesBucket, {originAccessIdentity: oai});
        var s3Origin=new S3Origin(staticResourcesBucket);
        var distribution=new Distribution(this, name, {
            defaultBehavior: { origin: s3Origin },
            defaultRootObject: "index.html"
        });
        //staticResourcesBucket.grantRead(oai); // Seems to grant read by default
    }

}

using Pulumi;
using Pulumi.Aws.Ec2;
using Pulumi.Aws.Ec2.Inputs;
using Pulumi.Aws.Inputs;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

class MyStack : Stack
{
    //My whole goal here is to be able to add Tags to the resulting EC2 instance that is created from the SpotRequest below in ONE `pulumi up` rather than having to do multiple steps. 
    //I understand that the limitation to add Tags to the resulting instance from the SpotInstanceRequest is an AWS limiation itself.
    //But I'm hoping that we can still have a way to add Tags to the resulting instance in the same vein / single step.

    [Output]
    public Output<string> SpotInstanceId { get; set; }

    public MyStack()
    {
        var ami = Pulumi.Aws.GetAmi.InvokeAsync(new Pulumi.Aws.GetAmiArgs
        {
            Filters =
                {
                    new GetAmiFilterArgs
                    {
                        Name = "name",
                        Values =  { "amzn-ami-hvm-*" },
                    },
                },
            Owners = { "137112412989" }, // This owner ID is Amazon
            MostRecent = true,
        }).Result;

        var securityGroup = new Pulumi.Aws.Ec2.SecurityGroup($"webserver-secgrp-1", new SecurityGroupArgs
        {
            Ingress = new SecurityGroupIngressArgs { Protocol = "tcp", FromPort = 22, ToPort = 22, CidrBlocks = { "0.0.0.0/0" } }
        });

        var spotRequest = new Pulumi.Aws.Ec2.SpotInstanceRequest($"dev-request-1", new SpotInstanceRequestArgs
        {
            Ami = ami.ImageId,
            InstanceType = "t2.micro",
            SpotPrice = "0.04",
            Tags = { { "Name", "testing" }, { "env", "dev" }, },
            WaitForFulfillment = true
        });

        //What is the best way from here on out to add Tags to the resulting SpotInstance created from the resource above?

        //---------------- OPTION 1 ----------------
        //Do some sort of wait in a for loop to check for the spotRequest.SpotInstanceId? 
        //If I could get access to the SpotInstanceId, I can leverage the AWS SDK probably to tag the instances.
        //The problem is, I can never get the value of the SpotInstanceId, everything I try just returns: Pulumi.Output`1[System.String]

        //if (!Deployment.Instance.IsDryRun)
        //{
        //    Console.WriteLine($"SpotInstanceId: {spotRequest.SpotInstanceId}");
        //    Console.WriteLine($"SpotInstanceId: {spotRequest.SpotInstanceId.ToString()}");
        //    Console.WriteLine($"SpotInstanceId: {spotRequest.SpotInstanceId.Apply(x => x)}");
        //    Console.WriteLine($"SpotInstanceId: {spotRequest.SpotInstanceId.Apply(x => x.ToString())}");
        //    Console.WriteLine($"SpotInstanceId: {spotRequest.SpotInstanceId.Apply(x => x).ToString()}");

        //    WaitAndTry(spotRequest).Wait();
        //}

        //---------------- END OPTION 1 ---------------



        //---------------- OPTION 2 --------------------
        //Do I have to do a multi-step process to import the resulting instance ID, and then update the tags on the instance that I import? 
        
        //Output the SpotInstanceId
        SpotInstanceId = spotRequest.SpotInstanceId;

        //Run this THE SECOND TIME you run the `pulumi up` command
        ////var stackReference = new StackReference($"replace/with-your/stack");
        ////var instanceId = stackReference.GetValueAsync("SpotInstanceId").Result.ToString();

        ////var ec2Instance = new Pulumi.Aws.Ec2.Instance($"imported-instance", new InstanceArgs
        ////{
        ////    Ami = ami.ImageId,
        ////    InstanceType = "t2.micro",
        ////}, new CustomResourceOptions
        ////{
        ////    DependsOn = spotRequest,
        ////    ImportId = instanceId
        ////});


        //Run this THE THIRD TIME you run the `pulumi up` command
        //////var ec2Instance = new Pulumi.Aws.Ec2.Instance($"imported-instance", new InstanceArgs
        //////{
        //////    Ami = ami.ImageId,
        //////    InstanceType = "t2.micro",
        //////    Tags = { { "Name", "testing" }, { "env", "dev" }, },
        //////}, new CustomResourceOptions
        //////{
        //////    DependsOn = spotRequest,
        //////});


        //---------------- END OPTION 2 ---------------------
    }

    private async Task WaitAndTry(SpotInstanceRequest spotRequest)
    {
        for (int i = 1; i <= 6; i++)
        {
            await Task.Delay(10000);

            var getSpotInstanceRequest = SpotInstanceRequest.Get($"get-spot-instance-{i}", spotRequest.Id, null, new CustomResourceOptions { DependsOn = spotRequest });

            Console.WriteLine($"SpotInstanceId from GET request: {getSpotInstanceRequest.SpotInstanceId}");
            Console.WriteLine($"SpotInstanceId from GET request: {getSpotInstanceRequest.SpotInstanceId.ToString()}");
            Console.WriteLine($"SpotInstanceId from GET request: {getSpotInstanceRequest.SpotInstanceId.Apply(x => x)}");
            Console.WriteLine($"SpotInstanceId from GET request: {getSpotInstanceRequest.SpotInstanceId.Apply(x => x.ToString())}");
            Console.WriteLine($"SpotInstanceId from GET request: {getSpotInstanceRequest.SpotInstanceId.Apply(x => x).ToString()}");

            //If I could actually get the SpotInstanceId here, I would be able to invoke the AWS SDK here to add tags to that instance?
        }
    }
}

TR23.TrueFX is a demonstration project I wrote for my upcoming "Batch Processing in Azure" presentation at Microsoft's internal TechReady23 conference.  The project is meant to demonstrate a number of real-world WebJobs features, including:

 - AmbientConnectionStringProvider
 - WebJob configuration
 - Working with TimerTriggers
 - Working with QueueTriggers 
 - Working with Queues
 - Working with CloudBlobContainers
 - Custom error-handling
 - Work decomposition
 - Sending email alerts via SendGrid
 - Dev vs. Prod Initialization
 - Graceful shutdown
 - Error aggregation via ErrorTrigger

To run the demo, you'll need to replace the AzureWebJobsSendGridApiKey, AlertsFrom, AlertsTo, AzureWebJobsDashboard and AzureWebJobsStorage placeholders in the App.config file with appropriate values.  Also, you'll need to sign up for a free SendGrid account at [https://sendgrid.com/pricing/](https://sendgrid.com/pricing/) then create an API Key.
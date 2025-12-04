# AWS SES Setup Guide

This guide explains how to set up AWS Simple Email Service (SES) for sending password reset emails.

## Prerequisites

- AWS Account with appropriate permissions
- Access to AWS Console or AWS CLI
- A verified email address or domain

## Step 1: Verify Email Address (Development/Testing)

For development and testing, you need to verify the sender email address:

1. Go to AWS SES Console: https://console.aws.amazon.com/ses/
2. Click on "Verified identities" in the left sidebar
3. Click "Create identity"
4. Select "Email address"
5. Enter your sender email (e.g., `noreply@yourdomain.com`)
6. Click "Create identity"
7. Check your email inbox and click the verification link

## Step 2: Verify Domain (Production)

For production, it's recommended to verify your entire domain:

1. Go to AWS SES Console
2. Click on "Verified identities"
3. Click "Create identity"
4. Select "Domain"
5. Enter your domain name (e.g., `yourdomain.com`)
6. Follow the instructions to add DNS records (DKIM, SPF, DMARC)
7. Wait for verification (can take up to 72 hours)

## Step 3: Request Production Access

By default, AWS SES is in "Sandbox mode" which limits:
- You can only send to verified email addresses
- Maximum 200 emails per 24 hours
- Maximum 1 email per second

To remove these limits:

1. Go to AWS SES Console
2. Click on "Account dashboard"
3. Click "Request production access"
4. Fill out the form:
   - **Mail type**: Transactional
   - **Website URL**: Your application URL
   - **Use case description**: 
     ```
     We use AWS SES to send transactional emails for our booking platform:
     - Password reset emails
     - Account verification emails
     - Booking confirmations
     - Notifications
     
     We implement proper email authentication (SPF, DKIM, DMARC) and 
     handle bounces and complaints appropriately.
     ```
   - **Compliance**: Confirm you comply with AWS policies
5. Submit the request
6. Wait for approval (usually 24-48 hours)

## Step 4: Configure Application

### Development (appsettings.Development.json)

```json
{
  "Email": {
    "SenderEmail": "noreply@yourdomain.com",
    "SenderName": "BookSpot Dev"
  },
  "App": {
    "BaseUrl": "https://localhost:5001"
  }
}
```

### Production (Environment Variables in Lambda)

Add these environment variables to your Lambda function:

```bash
Email__SenderEmail=noreply@yourdomain.com
Email__SenderName=BookSpot
App__BaseUrl=https://api.yourdomain.com
```

Or update your Terraform variables:

```hcl
# terraform/environments/prod.tfvars
sender_email = "noreply@yourdomain.com"
sender_name  = "BookSpot"
app_base_url = "https://api.yourdomain.com"
```

## Step 5: Test Email Sending

### Using AWS CLI

```bash
aws ses send-email \
  --from noreply@yourdomain.com \
  --destination ToAddresses=test@example.com \
  --message Subject={Data="Test Email",Charset=utf8},Body={Text={Data="This is a test",Charset=utf8}}
```

### Using Your API

```bash
curl -X POST https://your-api-url/auth/forgot-password \
  -H "Content-Type: application/json" \
  -d '{"email": "test@example.com"}'
```

## Step 6: Monitor Email Sending

### CloudWatch Metrics

Monitor these metrics in CloudWatch:
- `NumberOfMessagesAttempted`
- `NumberOfMessagesSent`
- `NumberOfMessagesRejected`
- `Bounce` rate
- `Complaint` rate

### Set Up Bounce and Complaint Handling

1. Create SNS topics for bounces and complaints
2. Configure SES to publish to these topics
3. Subscribe your application to handle bounces/complaints

```bash
# Create SNS topics
aws sns create-topic --name ses-bounces
aws sns create-topic --name ses-complaints

# Configure SES notifications
aws ses set-identity-notification-topic \
  --identity yourdomain.com \
  --notification-type Bounce \
  --sns-topic arn:aws:sns:region:account:ses-bounces

aws ses set-identity-notification-topic \
  --identity yourdomain.com \
  --notification-type Complaint \
  --sns-topic arn:aws:sns:region:account:ses-complaints
```

## Troubleshooting

### Email Not Received

1. **Check SES Sandbox Mode**: Verify recipient email is verified if in sandbox
2. **Check Spam Folder**: Emails might be filtered as spam
3. **Check CloudWatch Logs**: Look for errors in Lambda logs
4. **Verify IAM Permissions**: Ensure Lambda has `ses:SendEmail` permission
5. **Check SES Sending Statistics**: Look for bounces or rejections

### Common Errors

**Error: Email address is not verified**
- Solution: Verify the sender email in SES console

**Error: Daily sending quota exceeded**
- Solution: Request production access or wait 24 hours

**Error: Access Denied**
- Solution: Add SES permissions to Lambda IAM role

## Best Practices

1. **Use a dedicated sending domain**: Don't use your main domain
2. **Implement SPF, DKIM, and DMARC**: Improve deliverability
3. **Monitor bounce and complaint rates**: Keep them below 5% and 0.1%
4. **Handle unsubscribes**: Provide an unsubscribe mechanism
5. **Use email templates**: Store templates in SES for consistency
6. **Rate limiting**: Respect SES sending limits
7. **Retry logic**: Implement exponential backoff for failures

## Cost Estimation

AWS SES Pricing (as of 2024):
- First 62,000 emails per month: **FREE** (when sent from EC2/Lambda)
- After that: $0.10 per 1,000 emails
- Data transfer: Standard AWS rates

Example:
- 10,000 emails/month: **FREE**
- 100,000 emails/month: ~$3.80/month
- 1,000,000 emails/month: ~$93.80/month

## Additional Resources

- [AWS SES Documentation](https://docs.aws.amazon.com/ses/)
- [SES Best Practices](https://docs.aws.amazon.com/ses/latest/dg/best-practices.html)
- [Email Authentication](https://docs.aws.amazon.com/ses/latest/dg/email-authentication-methods.html)
- [SES Sending Limits](https://docs.aws.amazon.com/ses/latest/dg/manage-sending-quotas.html)

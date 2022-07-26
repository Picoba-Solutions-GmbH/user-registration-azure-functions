# Repository hints

- To set up this repository you need to define the variables in [local.settings.json](./SimpleUserManagement/local.settings.json) to be able to debug locally. <b>Important</b>: Never push this file with valid keys and secrets on it
- Some Function entry points have a commented out line like this
    ```csharp
    var jsonData = aesDecryption.DecryptCipherText(requestBody);
    ```
    Use this, if you want to transmit critical data encrypted via the internet. For testing with REST clients it is easier to disable this option
- At some points you'll see a reference to a function URL like in line number 82 in [ActivationTokenManager](./SimpleUserManagement/Implementations/ActivationTokenManager.cs). You have to adapt those when you deploy your function. Better would be to pass those URLs via environment variables

# Simple user registration with Azure Functions. 

Microsoft Azure Functions allows to easily build workflows. We used it to implement a simple user registration. Since we wanted to keep the registration lean, we didn't use powerful tools like Identity Server, which can do much more than what we need.

From the user's point of view the registration starts with an input dialog where a user enters name, email and possibly other information. Upon submitting the data, a confirmation link via email is sent to the user, which needs to be confirmed to complete the registration.

## Necessary Azure Functions

On Azure, first we need a Table in Table Storage to persist data, a Function to register a user, a Function to activate the user and a Function to validate the login.

In our Azure Subscription we create a user table in Table Storage to hold all the important user data.

![image.png](/.attachments/image-558c8d36-09b4-4069-b4b9-5f449cf44503.png)

In the registration function, the Azure Function creates a table entry of the user with a unique activation token. This token is appended to the activation link that is emailed to the user.

[RegisterUserAsync](./SimpleUserManagement/Implementations/ActivationTokenManager.cs)

The second function receives the activation message from the user and checks if the user exists in the table and activates the user.

[ActivateUserAsync](./SimpleUserManagement/Implementations/ActivationTokenManager.cs)

The third function checks if a user exists, is activated and the password is correct. This is a simple user login flow which is only exemplary. For security reasons we recommend a flow like OAuth2 or OIDC.

[ValidateLoginData](./SimpleUserManagement/Implementations/UserValidator.cs)

Here again an overview how simple the flow looks like:

![image.png](/.attachments/image-d56f886a-af65-421e-805c-6f2b486db75a.png)

## Send e-mails

After the flow is implemented, emails still need to be sent.

Initial research points to Sendgrid. Sendgrid in itself is good, but we wanted to save ourselves another system and besides, the free edition currently supports only 100 emails per day that can be sent. Further research recommends SMTP, which, however, is not directly supported by Azure Function. This would have required an SMTP relay, which would have complicated handling. We also want to use a shared mailbox. This would have required several more elaborate settings so that SMTP access would work.

Finally, we came to the Microsoft Graph API, which would be perfect for this and offers a quick solution for our problem. That's what we decided on.

### Requirements for sending e-mails

In order to be able to send emails via the Graph API, an Office365 account is required. There must also be a shared mailbox or a user who has the license to send e-mails.

### Setup

The first step is to create an app in the "App Registration" on Azure.

![image.png](/.attachments/image-dbcb3c43-a870-4b6a-8957-946c73e0fc8e.png)

In the API Permissions the App Registration must have the permission to send emails

![image.png](/.attachments/image-de8522f4-99de-49fb-bf10-adeda1d11337.png)

Finally, a client secret must be created under "Certificates & Secrets". This secret is needed in the following code section.

The following code shows how it is possible to send emails via the Microsoft Graph API on behalf of the shared mailbox.

[SendMailAsync](./SimpleUserManagement/Implementations/EmailSender.cs)
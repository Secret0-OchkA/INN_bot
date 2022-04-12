using API_Nalog;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

var botClient = new TelegramBotClient("");
using var cts = new CancellationTokenSource();

var receiverOptions = new ReceiverOptions
{
    AllowedUpdates = { }
};
botClient.StartReceiving(
    HandleUpdateAsync,
    HandleErrorAsync,
    receiverOptions,
    cancellationToken: cts.Token);



var me = await botClient.GetMeAsync();

Console.WriteLine($"Start listening for @{me.Username}");
Console.ReadLine();

cts.Cancel();



async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    // Only process Message updates: https://core.telegram.org/bots/api#message
    if (update.Type != UpdateType.Message)
        return;
    // Only process text messages
    if (update.Message!.Type != MessageType.Text)
        return;

    var chatId = update.Message.Chat.Id;
    var messageText = update.Message.Text;
    string response = string.Empty;
    Regex regex = new Regex(@"^[\d]+\s[0-9]{2}.[0-9]{2}.[0-9]{4}");

    if (messageText == "/help")
    {
        response = "Укажите ИНН физического лица или индивидуального\n" +
                   " предпринимателя и дату на которую нужно узнать статус в виде\n" +
                   "ИНН дд.мм.гггг\n" +
                   "Пример: 1452976843 10.01.2022";
    }
    else if(messageText != null && regex.IsMatch(messageText))
    {
        var result = messageText.Split(' ');
        string inn = result[0];
        DateTime dateRequest = DateTime.ParseExact(result[1], "dd.MM.yyyy",
                                                   System.Globalization.CultureInfo.InvariantCulture);
        StatusController statusController = new StatusController(inn, dateRequest);

        try
        {
            NalogResponse? status = await statusController.GetContentAsync();

            if (status != null)
                response = status.message;
        }
        catch (TaskCanceledException)
        {
            response = "по данному ИНН не получилось найти информации";
        }
    }
    else 
    {
        response = "Пожайлуста ввведите данные в формате ИНН дд.мм.гггг";
    }

    Message message = await botClient.SendTextMessageAsync(
    chatId: update.Message.Chat.Id,
    text: response,
    cancellationToken: cancellationToken);

    Console.WriteLine($"Received a '{messageText}' message in chat {chatId}.");
}

Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
{
    var ErrorMessage = exception switch
    {
        ApiRequestException apiRequestException
            => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
        _ => exception.ToString()
    };

    Console.WriteLine(ErrorMessage);
    return Task.CompletedTask;
}



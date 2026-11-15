namespace Assets.Scripts.BoredGames.API.Responses
{
    [System.Serializable]
    public class ErrorBadRequestResponse : IResponse
    {
        public Errors errors;
        public string title;
        public int status;

        public override string ToString()
        {
            var message = string.Empty;
            foreach (var item in errors.failedOperation)
            {
                message += item + System.Environment.NewLine;
            }
            return message;
        }
    }

    [System.Serializable]
    public class Errors
    {
        public string[] failedOperation;
    }
}
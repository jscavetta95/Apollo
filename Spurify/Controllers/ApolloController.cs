using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;


namespace Spurify.Controllers
{
    public class ApolloController : Controller
    {
        public const string LOGGEDIN_SESSION = "LoggedIn";
        public const string LOGIN_ERROR_SESSION = "LoginError";

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Login() {
            return View();
        }

        public void LoginDBHandler(string loginUsername, string loginPassword) {

            string username = 
                $username = escape_string($_POST["login_username"]);
    $password = escape_string(hashPassword($_POST["login_password"]));

            /* Validate Forms */
            if (strlen($username) > 20) {
        $errorMessage = "Username must not be greater then 20 characters";
            } else if (strlen($username) <= 0 || strlen($password) <= 0) {
        $errorMessage = "All forms must be filled";
            } else {
        $sql = "SELECT * FROM login WHERE username = '$username' AND password = '$password'";
        $result = runQuery($sql);
                if (mysqli_num_rows($result) == 1) {
                    session_start();
            $_SESSION['login'] = "1";
            $_SESSION['username'] = $_POST["login_username"];
                    header("Location: ../discover");
                    exit();
                } else {
            $errorMessage = "Invalid username or password";
                }
            }
            session_start();
    $_SESSION['login_register_Error'] = $errorMessage;
            header("Location: index.html");
            exit();
        }

        public void RegisterDBHandler(string regUsername, string regPassword, string regEmail) {

        }

        private void ConnectToDB() {
            string connectionString = ConfigurationManager.AppSettings["DatabaseConnectionString"];
            MySqlConnection dbConnection = new MySqlConnection(connectionString);
        }

    }
}
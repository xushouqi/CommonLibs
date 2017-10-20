using System;
using System.Collections.Generic;
using System.Text;

namespace CommonLibs
{
    public enum ErrorCodeEnum
    {
        Success = 0,

        ResponseError = 999,
        Unknown = 1000,
        WrongTokenOrTimeout = 1001,
        NotExists,
        UnAuthorized,
        Expired,
        NotValid,
        TimeOut,
        Disconnected,
        NoAction,

        WrongPassword,
        InvalidDataformat,

        NoMoney,
        NoDiamond,
        NoPower,

        NameExists,
        AlreadyExists,
        HasSameValue,
        NoTeam,

        CreateLeagueFail,
        CreateTeamFail,

        MatchNotFinish,
        MatchCantStart,
        MatchCantFinish,
        MatchAlreadyStart,
        MatchAlreadyFinish,

        PlayerChangeTeamFail,
        PlayerChangeStateFail,
        TransMoneyFail,
        PlayerNotOnListing,

        TeamGradeNotValid,
        NotValidSchedule,

        //UserNameOrIDCodeExists = 1001,
        //WrongUserOrPassword,
        //TodoItemNameAndNotesRequired,
        //TodoItemIDInUse,
        //RecordNotFound,
        //CouldNotCreateItem,
        //CouldNotUpdateItem,
        //CouldNotDeleteItem
    }

}

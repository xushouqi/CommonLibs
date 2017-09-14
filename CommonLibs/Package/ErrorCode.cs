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

        WrongPassword,
        InvalidDataformat,

        NoMoney,
        NoDiamond,
        NoPower,

        NameExists,
        AlreadyExists,
        HasSameValue,
        NoTeam,
        NotValidSchedule,

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

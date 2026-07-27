using OpenShock.Common.Problems;

namespace OpenShock.Common.Results;

// Generic structural unions used throughout the codebase in place of OneOf<T0, T1, ...>.
// These compose arbitrary existing types into a closed, exhaustively-matchable set,
// mirroring how OneOf<T0..Tn> was used. See: https://github.com/dotnet/csharplang/blob/main/proposals/unions.md

public union Union2<T0, T1>(T0, T1);

public union Union3<T0, T1, T2>(T0, T1, T2);

public union Union4<T0, T1, T2, T3>(T0, T1, T2, T3);

public union Union5<T0, T1, T2, T3, T4>(T0, T1, T2, T3, T4);

public union Union6<T0, T1, T2, T3, T4, T5>(T0, T1, T2, T3, T4, T5);

public union Union7<T0, T1, T2, T3, T4, T5, T6>(T0, T1, T2, T3, T4, T5, T6);

public union Union8<T0, T1, T2, T3, T4, T5, T6, T7>(T0, T1, T2, T3, T4, T5, T6, T7);

public union SuccessOrError<T>(Success, T);
public union SuccessOrProblem(Success, OpenShockProblem);
public union SuccessOrNotFound(Success, NotFound);

public union ValueOrProblem<T>(T, OpenShockProblem);

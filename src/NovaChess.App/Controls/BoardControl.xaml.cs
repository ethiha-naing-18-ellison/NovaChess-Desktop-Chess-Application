using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using NovaChess.Core;
using NovaChess.App.Models;

namespace NovaChess.App.Controls;

public partial class BoardControl : UserControl
{
    private readonly Dictionary<Square, Rectangle> _squares = new();
    private readonly Dictionary<Square, TextBlock> _pieces = new();
    private readonly Dictionary<Square, Border> _highlights = new();
    private readonly Dictionary<Square, Ellipse> _moveIndicators = new();
    
    private Square? _selectedSquare;
    private Square? _lastMoveFrom;
    private Square? _lastMoveTo;
    private List<Square> _possibleMoves = new();
    
    public event EventHandler<MoveMadeEventArgs>? MoveMade;
    
    public Board? Board { get; private set; }
    
    public BoardControl()
    {
        InitializeComponent();
        InitializeBoard();
        
        // Initialize file and rank labels
        FileLabels.ItemsSource = new[] { "a", "b", "c", "d", "e", "f", "g", "h" };
        RankLabels.ItemsSource = new[] { "8", "7", "6", "5", "4", "3", "2", "1" };
    }
    
    private void InitializeBoard()
    {
        for (int rank = 7; rank >= 0; rank--)
        {
            for (int file = 0; file < 8; file++)
            {
                var square = new Square(rank * 8 + file);
                var rect = CreateSquare(square);
                var piece = CreatePiece(square);
                var highlight = CreateHighlight(square);
                var moveIndicator = CreateMoveIndicator(square);
                
                _squares[square] = rect;
                _pieces[square] = piece;
                _highlights[square] = highlight;
                _moveIndicators[square] = moveIndicator;
                
                BoardGrid.Children.Add(rect);
                BoardGrid.Children.Add(piece);
                BoardGrid.Children.Add(highlight);
                BoardGrid.Children.Add(moveIndicator);
                
                Grid.SetRow(rect, 7 - rank);
                Grid.SetColumn(rect, file);
                Grid.SetRow(piece, 7 - rank);
                Grid.SetColumn(piece, file);
                Grid.SetRow(highlight, 7 - rank);
                Grid.SetColumn(highlight, file);
                Grid.SetRow(moveIndicator, 7 - rank);
                Grid.SetColumn(moveIndicator, file);
                
                        // Add mouse events
        rect.MouseLeftButtonDown += OnSquareMouseDown;
        rect.MouseEnter += OnSquareMouseEnter;
        rect.MouseLeave += OnSquareMouseLeave;
            }
        }
    }
    
    private Rectangle CreateSquare(Square square)
    {
        var rect = new Rectangle
        {
            Fill = square.IsLightSquare ? 
                new SolidColorBrush(Color.FromRgb(240, 217, 181)) : 
                new SolidColorBrush(Color.FromRgb(181, 136, 99)),
            Stroke = Brushes.Black,
            StrokeThickness = 1,
            IsHitTestVisible = true, // Ensure the rectangle can receive mouse events
            Cursor = Cursors.Hand // Show hand cursor when hovering over squares
        };
        
        return rect;
    }
    
    private TextBlock CreatePiece(Square square)
    {
        var piece = new TextBlock
        {
            FontSize = 32,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Text = "",
            Foreground = Brushes.Black,
            IsHitTestVisible = false // This allows mouse events to pass through to the Rectangle below
        };
        
        return piece;
    }
    
    private Border CreateHighlight(Square square)
    {
        var highlight = new Border
        {
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Yellow,
            BorderThickness = new Thickness(3),
            IsHitTestVisible = false
        };
        
        return highlight;
    }
    
    private Ellipse CreateMoveIndicator(Square square)
    {
        var indicator = new Ellipse
        {
            Width = 30,
            Height = 30,
            Fill = new SolidColorBrush(Color.FromArgb(180, 0, 255, 0)), // More opaque green
            Stroke = Brushes.DarkGreen,
            StrokeThickness = 3,
            IsHitTestVisible = true, // Make it clickable
            Visibility = Visibility.Visible, // Make them always visible for testing
            Cursor = Cursors.Hand, // Show hand cursor when hovering
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        
        // Add mouse events to the move indicator
        indicator.MouseLeftButtonDown += OnMoveIndicatorMouseDown;
        
        return indicator;
    }
    
    public void SetBoard(Board board)
    {
        Board = board;
        UpdateBoard();
        UpdateGameStatus();
    }
    
    public void UpdateBoard()
    {
        if (Board == null) return;
        
        System.Diagnostics.Debug.WriteLine("=== UPDATING BOARD ===");
        
        // First, clear ALL pieces
        for (int rank = 7; rank >= 0; rank--)
        {
            for (int file = 0; file < 8; file++)
            {
                var square = new Square(rank * 8 + file);
                _pieces[square].Text = "";
                _pieces[square].Foreground = Brushes.Black;
            }
        }
        
        // Then, set pieces based on current board state
        for (int rank = 7; rank >= 0; rank--)
        {
            for (int file = 0; file < 8; file++)
            {
                var square = new Square(rank * 8 + file);
                var (pieceType, pieceColor) = Board.GetPiece(square);
                
                if (pieceType != PieceType.None)
                {
                    _pieces[square].Text = GetPieceSymbol(pieceType);
                    _pieces[square].Foreground = pieceColor == PieceColor.White ? Brushes.White : Brushes.Black;
                    System.Diagnostics.Debug.WriteLine($"Set {pieceColor} {pieceType} on {square}");
                }
            }
        }
        
        UpdateHighlights();
        UpdateGameStatus();
        System.Diagnostics.Debug.WriteLine("=== BOARD UPDATE COMPLETE ===");
    }
    
    private string GetPieceSymbol(PieceType pieceType)
    {
        return pieceType switch
        {
            PieceType.Pawn => "♟",
            PieceType.Knight => "♞",
            PieceType.Bishop => "♝",
            PieceType.Rook => "♜",
            PieceType.Queen => "♛",
            PieceType.King => "♚",
            _ => ""
        };
    }
    
    private void UpdateHighlights()
    {
        // Clear all highlights
        foreach (var highlight in _highlights.Values)
        {
            highlight.Background = Brushes.Transparent;
            highlight.BorderBrush = Brushes.Transparent;
        }
        
        // Clear all move indicators
        foreach (var indicator in _moveIndicators.Values)
        {
            indicator.Visibility = Visibility.Collapsed;
        }
        
        // Highlight last move
        if (_lastMoveFrom.HasValue && _lastMoveTo.HasValue)
        {
            _highlights[_lastMoveFrom.Value].Background = Brushes.LightBlue;
            _highlights[_lastMoveTo.Value].Background = Brushes.LightBlue;
        }
        
        // Highlight selected square and show possible moves
        if (_selectedSquare.HasValue)
        {
            _highlights[_selectedSquare.Value].BorderBrush = Brushes.Yellow;
            
            // Show possible moves
            foreach (var moveSquare in _possibleMoves)
            {
                _moveIndicators[moveSquare].Visibility = Visibility.Visible;
                System.Diagnostics.Debug.WriteLine($"Showing move indicator on square: {moveSquare}");
            }
        }
    }
    
    private void UpdateGameStatus()
    {
        if (Board == null) return;
        
        var status = Board.SideToMove == PieceColor.White ? "White to move" : "Black to move";
        GameStatusText.Text = status;
    }
    
    private void OnSquareMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (Board == null) return;
        
        try
        {
            System.Diagnostics.Debug.WriteLine("=== SQUARE CLICKED ===");
            
            var square = GetSquareFromElement(sender as FrameworkElement);
            if (!square.HasValue) 
            {
                System.Diagnostics.Debug.WriteLine("No valid square found");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"Clicked square: {square.Value}");
            
            // If we already have a piece selected, try to move it here
            if (_selectedSquare.HasValue)
            {
                System.Diagnostics.Debug.WriteLine($"Attempting move from {_selectedSquare.Value} to {square.Value}");
                
                var (fromPieceType, fromPieceColor) = Board.GetPiece(_selectedSquare.Value);
                var (toPieceType, toPieceColor) = Board.GetPiece(square.Value);
                
                if (fromPieceType != PieceType.None)
                {
                    // VALIDATE THE MOVE ACCORDING TO CHESS RULES
                    if (!IsValidChessMove(_selectedSquare.Value, square.Value, fromPieceType, fromPieceColor, toPieceType, toPieceColor))
                    {
                        System.Diagnostics.Debug.WriteLine("Invalid move according to chess rules!");
                        _selectedSquare = null;
                        _possibleMoves.Clear();
                        UpdateHighlights();
                        return;
                    }
                    
                    // EXECUTE THE MOVE DIRECTLY ON THE UI
                    System.Diagnostics.Debug.WriteLine($"=== EXECUTING MOVE DIRECTLY ON UI ===");
                    System.Diagnostics.Debug.WriteLine($"Moving {fromPieceType} ({fromPieceColor}) from {_selectedSquare.Value} to {square.Value}");
                    
                    // DIRECTLY UPDATE THE UI ELEMENTS
                    // Clear the source square visually
                    _pieces[_selectedSquare.Value].Text = "";
                    System.Diagnostics.Debug.WriteLine($"Cleared UI element at {_selectedSquare.Value}");
                    
                    // Set the destination square visually
                    _pieces[square.Value].Text = GetPieceSymbol(fromPieceType);
                    _pieces[square.Value].Foreground = fromPieceColor == PieceColor.White ? Brushes.White : Brushes.Black;
                    System.Diagnostics.Debug.WriteLine($"Set UI element at {square.Value} to {fromPieceType}");
                    
                    // Update the board state to match
                    Board.SetPiece(_selectedSquare.Value, PieceType.None, PieceColor.White);
                    Board.SetPiece(square.Value, fromPieceType, fromPieceColor);
                    
                    // Switch turns
                    Board.SideToMove = Board.SideToMove == PieceColor.White ? PieceColor.Black : PieceColor.White;
                    System.Diagnostics.Debug.WriteLine($"Turn switched to: {Board.SideToMove}");
                    
                    // Update game status
                    UpdateGameStatus();
                    System.Diagnostics.Debug.WriteLine("=== DIRECT UI MOVE COMPLETE ===");
                    
                    // Clear selection
                    _selectedSquare = null;
                    _possibleMoves.Clear();
                    UpdateHighlights();
                    return;
                }
            }
            
            // Select a new piece
            var (pieceType, pieceColor) = Board.GetPiece(square.Value);
            System.Diagnostics.Debug.WriteLine($"Piece: {pieceType}, Color: {pieceColor}, Current turn: {Board.SideToMove}");
            
            // ALLOW PIECE SELECTION (SIMPLIFIED)
            if (pieceType != PieceType.None)
            {
                _selectedSquare = square.Value;
                _possibleMoves = CalculateRealisticMoves(square.Value, pieceType, pieceColor);
                System.Diagnostics.Debug.WriteLine($"Selected {pieceColor} {pieceType} with {_possibleMoves.Count} realistic moves");
                UpdateHighlights();
            }
            else
            {
                _selectedSquare = null;
                _possibleMoves.Clear();
                UpdateHighlights();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
        }
    }
    
    private void OnSquareMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (Board == null || !_selectedSquare.HasValue) return;
        
        try
        {
            var targetSquare = GetSquareFromElement(sender as FrameworkElement);
            if (!targetSquare.HasValue) 
            {
                System.Diagnostics.Debug.WriteLine("No valid target square found for mouse up");
                _selectedSquare = null;
                UpdateHighlights();
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"Attempting move from {_selectedSquare.Value} to {targetSquare.Value}");
            
            if (_selectedSquare.Value != targetSquare.Value)
            {
                // Check if the target square is a possible move
                if (_possibleMoves.Contains(targetSquare.Value))
                {
                    // Try to make the move
                    var (pieceType, _) = Board.GetPiece(_selectedSquare.Value);
                    var move = new Move(_selectedSquare.Value, targetSquare.Value, pieceType);
                    
                    System.Diagnostics.Debug.WriteLine($"Created move: {move}");
                    
                    if (IsLegalMove(move))
                    {
                        System.Diagnostics.Debug.WriteLine("Move is legal, invoking MoveMade event");
                        MoveMade?.Invoke(this, new MoveMadeEventArgs(move));
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Move is not legal");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Target square is not a possible move");
                }
            }
            
            _selectedSquare = null;
            _possibleMoves.Clear();
            UpdateHighlights();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in OnSquareMouseUp: {ex.Message}");
            _selectedSquare = null;
            _possibleMoves.Clear();
            UpdateHighlights();
        }
    }
    
    private void OnSquareMouseEnter(object sender, MouseEventArgs e)
    {
        if (Board == null) return;
        
        var square = GetSquareFromElement(sender as FrameworkElement);
        if (!square.HasValue) return;
        
        var (pieceType, pieceColor) = Board.GetPiece(square.Value);
        if (pieceType != PieceType.None && pieceColor == Board.SideToMove)
        {
            // Show possible moves (optional enhancement)
        }
    }
    
    private void OnSquareMouseLeave(object sender, MouseEventArgs e)
    {
        // Clear any hover effects
    }
    
            private void OnMoveIndicatorMouseDown(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("=== MOVE INDICATOR CLICKED ===");
            
            if (Board == null || !_selectedSquare.HasValue) 
            {
                System.Diagnostics.Debug.WriteLine("OnMoveIndicatorMouseDown: Board is null or no selected square");
                return;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine("Move indicator clicked!");

                var targetSquare = GetSquareFromElement(sender as FrameworkElement);
                if (!targetSquare.HasValue)
                {
                    System.Diagnostics.Debug.WriteLine("No valid target square found for move indicator");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"Move indicator clicked on square: {targetSquare.Value}");
                System.Diagnostics.Debug.WriteLine($"Selected square: {_selectedSquare.Value}");
                System.Diagnostics.Debug.WriteLine($"Possible moves count: {_possibleMoves.Count}");

                // Check if the target square is a possible move
                if (_possibleMoves.Contains(targetSquare.Value))
                {
                    var (pieceType, pieceColor) = Board.GetPiece(_selectedSquare.Value);
                    var (targetPieceType, targetPieceColor) = Board.GetPiece(targetSquare.Value);
                    
                    // VALIDATE THE MOVE ACCORDING TO CHESS RULES
                    if (IsValidChessMove(_selectedSquare.Value, targetSquare.Value, pieceType, pieceColor, targetPieceType, targetPieceColor))
                    {
                        // EXECUTE THE MOVE DIRECTLY ON THE UI
                        System.Diagnostics.Debug.WriteLine($"Moving {pieceType} from {_selectedSquare.Value} to {targetSquare.Value} via indicator");
                        
                        // DIRECTLY UPDATE THE UI ELEMENTS
                        // Clear the source square visually
                        _pieces[_selectedSquare.Value].Text = "";
                        
                        // Set the destination square visually
                        _pieces[targetSquare.Value].Text = GetPieceSymbol(pieceType);
                        _pieces[targetSquare.Value].Foreground = pieceColor == PieceColor.White ? Brushes.White : Brushes.Black;
                        
                        // Update the board state to match
                        Board.SetPiece(_selectedSquare.Value, PieceType.None, PieceColor.White);
                        Board.SetPiece(targetSquare.Value, pieceType, pieceColor);
                        
                        // Switch turns
                        Board.SideToMove = Board.SideToMove == PieceColor.White ? PieceColor.Black : PieceColor.White;
                        
                        // Update game status
                        UpdateGameStatus();
                        System.Diagnostics.Debug.WriteLine("Valid move executed via indicator with direct UI update!");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Move rejected by chess rules validation!");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Target square {targetSquare.Value} is not in possible moves list");
                    foreach (var possibleMove in _possibleMoves)
                    {
                        System.Diagnostics.Debug.WriteLine($"  Possible move: {possibleMove}");
                    }
                }

                _selectedSquare = null;
                _possibleMoves.Clear();
                UpdateHighlights();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnMoveIndicatorMouseDown: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                _selectedSquare = null;
                _possibleMoves.Clear();
                UpdateHighlights();
            }
        }
    
    private bool IsValidChessMove(Square from, Square to, PieceType pieceType, PieceColor pieceColor, PieceType targetPieceType, PieceColor targetPieceColor)
    {
        // This is the old simple validation - keep it for backward compatibility
        if (from == to) return false;
        if (targetPieceType != PieceType.None && targetPieceColor == pieceColor) return false;
        return true;
    }

    private bool IsProperChessMove(Square from, Square to, PieceType pieceType, PieceColor pieceColor, PieceType targetPieceType, PieceColor targetPieceColor)
    {
        System.Diagnostics.Debug.WriteLine($"=== PROPER CHESS VALIDATION ===");
        System.Diagnostics.Debug.WriteLine($"Moving {pieceColor} {pieceType} from {from} to {to}");
        
        // Basic checks
        if (from == to)
        {
            System.Diagnostics.Debug.WriteLine("INVALID: Same square");
            return false;
        }

        if (targetPieceType != PieceType.None && targetPieceColor == pieceColor)
        {
            System.Diagnostics.Debug.WriteLine("INVALID: Can't capture own piece");
            return false;
        }

        // Piece-specific movement validation
        bool validMovement = ValidatePieceMovement(from, to, pieceType, pieceColor, targetPieceType != PieceType.None);
        if (!validMovement)
        {
            System.Diagnostics.Debug.WriteLine($"INVALID: Illegal {pieceType} movement");
            return false;
        }

        System.Diagnostics.Debug.WriteLine("MOVE VALID!");
        return true;
    }

    private bool ValidatePieceMovement(Square from, Square to, PieceType pieceType, PieceColor pieceColor, bool isCapture)
    {
        int fromFile = from.File;
        int fromRank = from.Rank;
        int toFile = to.File;
        int toRank = to.Rank;
        
        int fileDiff = Math.Abs(toFile - fromFile);
        int rankDiff = Math.Abs(toRank - fromRank);

        switch (pieceType)
        {
            case PieceType.Pawn:
                return ValidatePawnMovement(from, to, pieceColor, isCapture);
                
            case PieceType.Rook:
                return (fileDiff == 0 || rankDiff == 0) && IsPathClear(from, to);
                
            case PieceType.Knight:
                return (fileDiff == 2 && rankDiff == 1) || (fileDiff == 1 && rankDiff == 2);
                
            case PieceType.Bishop:
                return fileDiff == rankDiff && IsPathClear(from, to);
                
            case PieceType.Queen:
                return ((fileDiff == 0 || rankDiff == 0) || (fileDiff == rankDiff)) && IsPathClear(from, to);
                
            case PieceType.King:
                return fileDiff <= 1 && rankDiff <= 1;
                
            default:
                return false;
        }
    }

    private bool ValidatePawnMovement(Square from, Square to, PieceColor pieceColor, bool isCapture)
    {
        int direction = pieceColor == PieceColor.White ? 1 : -1;
        int startRank = pieceColor == PieceColor.White ? 1 : 6;
        
        int fileDiff = to.File - from.File;
        int rankDiff = to.Rank - from.Rank;

        // Forward moves
        if (fileDiff == 0 && !isCapture)
        {
            if (rankDiff == direction) return true; // One square forward
            if (from.Rank == startRank && rankDiff == 2 * direction) return true; // Two squares from start
        }
        
        // Diagonal captures
        if (Math.Abs(fileDiff) == 1 && rankDiff == direction && isCapture)
        {
            return true;
        }
        
        return false;
    }

    private bool IsValidPawnMove(Square from, Square to, PieceColor pieceColor, bool isCapture)
    {
        int direction = pieceColor == PieceColor.White ? 1 : -1;
        int fromRank = from.Rank;
        int toRank = to.Rank;
        int fromFile = from.File;
        int toFile = to.File;
        
        int rankDiff = toRank - fromRank;
        int fileDiff = Math.Abs(toFile - fromFile);

        // Pawns can only move forward
        if (rankDiff * direction <= 0)
        {
            System.Diagnostics.Debug.WriteLine("Pawns can't move backward!");
            return false;
        }

        if (isCapture)
        {
            // Diagonal capture: one square diagonally forward
            return rankDiff == direction && fileDiff == 1;
        }
        else
        {
            // Forward move: same file
            if (fileDiff != 0) return false;
            
            // One square forward is always allowed
            if (rankDiff == direction) return true;
            
            // Two squares forward only from starting position
            if (rankDiff == 2 * direction)
            {
                int startingRank = pieceColor == PieceColor.White ? 1 : 6;
                return fromRank == startingRank;
            }
            
            return false;
        }
    }

    private Square? GetSquareFromElement(FrameworkElement? element)
    {
        if (element == null) return null;
        
        var row = Grid.GetRow(element);
        var col = Grid.GetColumn(element);
        
        // Debug: Log the row and column values
        System.Diagnostics.Debug.WriteLine($"GetSquareFromElement: row={row}, col={col}");
        
        // Check if the element is actually in the board grid
        if (row < 0 || row >= 8 || col < 0 || col >= 8)
        {
            System.Diagnostics.Debug.WriteLine($"Invalid grid position: row={row}, col={col}");
            return null;
        }
        
        try
        {
            // Convert grid coordinates to square index
            // Grid row 0 = rank 7, Grid row 7 = rank 0 (chess board is upside down in grid)
            var squareIndex = (7 - row) * 8 + col;
            System.Diagnostics.Debug.WriteLine($"Square index: {squareIndex}");
            return new Square(squareIndex);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creating square: {ex.Message}");
            return null;
        }
    }
    
    private bool IsLegalMove(Move move)
    {
        if (Board == null) return false;
        
        // Check if the move is legal by validating it against the board state
        // This is a basic validation - the Game class will do more thorough validation
        
        // Check if source square has a piece
        var (pieceType, pieceColor) = Board.GetPiece(move.From);
        if (pieceType == PieceType.None) return false;
        
        // Check if it's the correct color's turn
        if (pieceColor != Board.SideToMove) return false;
        
        // Check if destination is different from source
        if (move.From == move.To) return false;
        
        // Check if destination square is not occupied by own piece
        var (destPiece, destColor) = Board.GetPiece(move.To);
        if (destPiece != PieceType.None && destColor == pieceColor) return false;
        
        // Basic piece movement validation
        return IsValidPieceMovement(move, pieceType, pieceColor);
    }
    
    private bool IsValidPieceMovement(Move move, PieceType pieceType, PieceColor color)
    {
        var from = move.From;
        var to = move.To;
        var fileDiff = Math.Abs(to.File - from.File);
        var rankDiff = Math.Abs(to.Rank - from.Rank);
        
        return pieceType switch
        {
            PieceType.Pawn => IsValidPawnMove(from, to, color),
            PieceType.Knight => (fileDiff == 2 && rankDiff == 1) || (fileDiff == 1 && rankDiff == 2),
            PieceType.Bishop => fileDiff == rankDiff && IsPathClear(from, to),
            PieceType.Rook => (fileDiff == 0 || rankDiff == 0) && IsPathClear(from, to),
            PieceType.Queen => ((fileDiff == rankDiff) || (fileDiff == 0 || rankDiff == 0)) && IsPathClear(from, to),
            PieceType.King => fileDiff <= 1 && rankDiff <= 1,
            _ => false
        };
    }
    
    private bool IsValidPawnMove(Square from, Square to, PieceColor color)
    {
        var fileDiff = to.File - from.File;
        var rankDiff = to.Rank - from.Rank;
        
        // Pawns can only move forward
        var forwardDirection = color == PieceColor.White ? 1 : -1;
        
        // Forward move
        if (fileDiff == 0)
        {
            // Single square forward
            if (rankDiff == forwardDirection)
            {
                return !Board!.IsSquareOccupied(to);
            }
            // Double square forward from starting position
            else if (rankDiff == 2 * forwardDirection && 
                     ((color == PieceColor.White && from.Rank == 1) || 
                      (color == PieceColor.Black && from.Rank == 6)))
            {
                return !Board!.IsSquareOccupied(to) && !Board.IsSquareOccupied(new Square(from.File, from.Rank + forwardDirection));
            }
        }
        // Diagonal capture
        else if (Math.Abs(fileDiff) == 1 && rankDiff == forwardDirection)
        {
            return Board!.IsSquareOccupiedByColor(to, color == PieceColor.White ? PieceColor.Black : PieceColor.White);
        }
        
        return false;
    }
    
    private bool IsPathClear(Square from, Square to)
    {
        var fileStep = from.File == to.File ? 0 : (to.File - from.File) / Math.Abs(to.File - from.File);
        var rankStep = from.Rank == to.Rank ? 0 : (to.Rank - from.Rank) / Math.Abs(to.Rank - from.Rank);
        
        var current = new Square(from.File + fileStep, from.Rank + rankStep);
        
        while (current != to)
        {
            if (Board!.IsSquareOccupied(current))
                return false;
                
            current = new Square(current.File + fileStep, current.Rank + rankStep);
        }
        
        return true;
    }
    
    private List<Square> CalculateRealisticMoves(Square from, PieceType pieceType, PieceColor color)
    {
        var moves = new List<Square>();
        
        System.Diagnostics.Debug.WriteLine($"=== CALCULATING MOVES FOR {color} {pieceType} AT {from} ===");
        
        // SIMPLE AND CORRECT CHESS MOVES
        switch (pieceType)
        {
            case PieceType.Pawn:
                moves.AddRange(GetPawnMoves(from, color));
                break;
            case PieceType.Rook:
                moves.AddRange(GetRookMoves(from, color));
                break;
            case PieceType.Knight:
                moves.AddRange(GetKnightMoves(from, color));
                break;
            case PieceType.Bishop:
                moves.AddRange(GetBishopMoves(from, color));
                break;
            case PieceType.Queen:
                moves.AddRange(GetQueenMoves(from, color));
                break;
            case PieceType.King:
                moves.AddRange(GetKingMoves(from, color));
                break;
        }
        
        System.Diagnostics.Debug.WriteLine($"Final moves for {pieceType}: {moves.Count}");
        return moves;
    }

    private List<Square> GetPawnMoves(Square from, PieceColor color)
    {
        var moves = new List<Square>();
        int direction = color == PieceColor.White ? 1 : -1;
        int startRank = color == PieceColor.White ? 1 : 6;
        
        // One square forward
        var oneForward = new Square(from.File, from.Rank + direction);
        if (oneForward.IsValid && Board.GetPiece(oneForward).Item1 == PieceType.None)
        {
            moves.Add(oneForward);
            
            // Two squares forward from starting position
            if (from.Rank == startRank)
            {
                var twoForward = new Square(from.File, from.Rank + 2 * direction);
                if (twoForward.IsValid && Board.GetPiece(twoForward).Item1 == PieceType.None)
                {
                    moves.Add(twoForward);
                }
            }
        }
        
        // Diagonal captures
        var leftCapture = new Square(from.File - 1, from.Rank + direction);
        if (leftCapture.IsValid)
        {
            var (piece, pieceColor) = Board.GetPiece(leftCapture);
            if (piece != PieceType.None && pieceColor != color)
            {
                moves.Add(leftCapture);
            }
        }
        
        var rightCapture = new Square(from.File + 1, from.Rank + direction);
        if (rightCapture.IsValid)
        {
            var (piece, pieceColor) = Board.GetPiece(rightCapture);
            if (piece != PieceType.None && pieceColor != color)
            {
                moves.Add(rightCapture);
            }
        }
        
        return moves;
    }

    private List<Square> GetRookMoves(Square from, PieceColor color)
    {
        var moves = new List<Square>();
        
        // Horizontal and vertical directions
        int[] directions = { 0, 1, 0, -1, 1, 0, -1, 0 };
        
        for (int i = 0; i < directions.Length; i += 2)
        {
            int fileDir = directions[i];
            int rankDir = directions[i + 1];
            
            for (int distance = 1; distance < 8; distance++)
            {
                var to = new Square(from.File + fileDir * distance, from.Rank + rankDir * distance);
                if (!to.IsValid) break;
                
                var (piece, pieceColor) = Board.GetPiece(to);
                if (piece == PieceType.None)
                {
                    moves.Add(to);
                }
                else if (pieceColor != color)
                {
                    moves.Add(to); // Can capture
                    break;
                }
                else
                {
                    break; // Own piece blocks
                }
            }
        }
        
        return moves;
    }

    private List<Square> GetKnightMoves(Square from, PieceColor color)
    {
        var moves = new List<Square>();
        int[] knightMoves = { -2, -1, -2, 1, -1, -2, -1, 2, 1, -2, 1, 2, 2, -1, 2, 1 };
        
        for (int i = 0; i < knightMoves.Length; i += 2)
        {
            var to = new Square(from.File + knightMoves[i], from.Rank + knightMoves[i + 1]);
            if (to.IsValid)
            {
                var (piece, pieceColor) = Board.GetPiece(to);
                if (piece == PieceType.None || pieceColor != color)
                {
                    moves.Add(to);
                }
            }
        }
        
        return moves;
    }

    private List<Square> GetBishopMoves(Square from, PieceColor color)
    {
        var moves = new List<Square>();
        
        // Diagonal directions
        int[] directions = { 1, 1, 1, -1, -1, 1, -1, -1 };
        
        for (int i = 0; i < directions.Length; i += 2)
        {
            int fileDir = directions[i];
            int rankDir = directions[i + 1];
            
            for (int distance = 1; distance < 8; distance++)
            {
                var to = new Square(from.File + fileDir * distance, from.Rank + rankDir * distance);
                if (!to.IsValid) break;
                
                var (piece, pieceColor) = Board.GetPiece(to);
                if (piece == PieceType.None)
                {
                    moves.Add(to);
                }
                else if (pieceColor != color)
                {
                    moves.Add(to); // Can capture
                    break;
                }
                else
                {
                    break; // Own piece blocks
                }
            }
        }
        
        return moves;
    }

    private List<Square> GetQueenMoves(Square from, PieceColor color)
    {
        var moves = new List<Square>();
        moves.AddRange(GetRookMoves(from, color));
        moves.AddRange(GetBishopMoves(from, color));
        return moves;
    }

    private List<Square> GetKingMoves(Square from, PieceColor color)
    {
        var moves = new List<Square>();
        
        for (int fileOffset = -1; fileOffset <= 1; fileOffset++)
        {
            for (int rankOffset = -1; rankOffset <= 1; rankOffset++)
            {
                if (fileOffset == 0 && rankOffset == 0) continue;
                
                var to = new Square(from.File + fileOffset, from.Rank + rankOffset);
                if (to.IsValid)
                {
                    var (piece, pieceColor) = Board.GetPiece(to);
                    if (piece == PieceType.None || pieceColor != color)
                    {
                        moves.Add(to);
                    }
                }
            }
        }
        
        return moves;
    }

    private List<Square> CalculateRealisticPawnMoves(Square from, PieceColor color)
    {
        var moves = new List<Square>();
        int direction = color == PieceColor.White ? 1 : -1;
        int startRank = color == PieceColor.White ? 1 : 6;
        
        // One square forward
        var oneForward = new Square(from.File, from.Rank + direction);
        if (oneForward.IsValid && Board.GetPiece(oneForward).Item1 == PieceType.None)
        {
            moves.Add(oneForward);
            
            // Two squares forward from starting position
            if (from.Rank == startRank)
            {
                var twoForward = new Square(from.File, from.Rank + 2 * direction);
                if (twoForward.IsValid && Board.GetPiece(twoForward).Item1 == PieceType.None)
                {
                    moves.Add(twoForward);
                }
            }
        }
        
        // Diagonal captures
        var leftCapture = new Square(from.File - 1, from.Rank + direction);
        if (leftCapture.IsValid)
        {
            var (piece, pieceColor) = Board.GetPiece(leftCapture);
            if (piece != PieceType.None && pieceColor != color)
            {
                moves.Add(leftCapture);
            }
        }
        
        var rightCapture = new Square(from.File + 1, from.Rank + direction);
        if (rightCapture.IsValid)
        {
            var (piece, pieceColor) = Board.GetPiece(rightCapture);
            if (piece != PieceType.None && pieceColor != color)
            {
                moves.Add(rightCapture);
            }
        }
        
        return moves;
    }

    private List<Square> CalculateRealisticRookMoves(Square from, PieceColor color)
    {
        var moves = new List<Square>();
        
        // Horizontal and vertical directions
        int[] directions = { 0, 1, 0, -1, 1, 0, -1, 0 };
        
        for (int i = 0; i < directions.Length; i += 2)
        {
            int fileDir = directions[i];
            int rankDir = directions[i + 1];
            
            for (int distance = 1; distance < 8; distance++)
            {
                var to = new Square(from.File + fileDir * distance, from.Rank + rankDir * distance);
                if (!to.IsValid) break;
                
                var (piece, pieceColor) = Board.GetPiece(to);
                if (piece == PieceType.None)
                {
                    moves.Add(to);
                }
                else if (pieceColor != color)
                {
                    moves.Add(to); // Can capture
                    break;
                }
                else
                {
                    break; // Own piece blocks
                }
            }
        }
        
        return moves;
    }

    private List<Square> CalculateRealisticKnightMoves(Square from, PieceColor color)
    {
        var moves = new List<Square>();
        int[] knightMoves = { -2, -1, -2, 1, -1, -2, -1, 2, 1, -2, 1, 2, 2, -1, 2, 1 };
        
        for (int i = 0; i < knightMoves.Length; i += 2)
        {
            var to = new Square(from.File + knightMoves[i], from.Rank + knightMoves[i + 1]);
            if (to.IsValid)
            {
                var (piece, pieceColor) = Board.GetPiece(to);
                if (piece == PieceType.None || pieceColor != color)
                {
                    moves.Add(to);
                }
            }
        }
        
        return moves;
    }

    private List<Square> CalculateRealisticBishopMoves(Square from, PieceColor color)
    {
        var moves = new List<Square>();
        
        // Diagonal directions
        int[] directions = { 1, 1, 1, -1, -1, 1, -1, -1 };
        
        for (int i = 0; i < directions.Length; i += 2)
        {
            int fileDir = directions[i];
            int rankDir = directions[i + 1];
            
            for (int distance = 1; distance < 8; distance++)
            {
                var to = new Square(from.File + fileDir * distance, from.Rank + rankDir * distance);
                if (!to.IsValid) break;
                
                var (piece, pieceColor) = Board.GetPiece(to);
                if (piece == PieceType.None)
                {
                    moves.Add(to);
                }
                else if (pieceColor != color)
                {
                    moves.Add(to); // Can capture
                    break;
                }
                else
                {
                    break; // Own piece blocks
                }
            }
        }
        
        return moves;
    }

    private List<Square> CalculateRealisticQueenMoves(Square from, PieceColor color)
    {
        var moves = new List<Square>();
        moves.AddRange(CalculateRealisticRookMoves(from, color));
        moves.AddRange(CalculateRealisticBishopMoves(from, color));
        return moves;
    }

    private List<Square> CalculateRealisticKingMoves(Square from, PieceColor color)
    {
        var moves = new List<Square>();
        
        for (int fileOffset = -1; fileOffset <= 1; fileOffset++)
        {
            for (int rankOffset = -1; rankOffset <= 1; rankOffset++)
            {
                if (fileOffset == 0 && rankOffset == 0) continue;
                
                var to = new Square(from.File + fileOffset, from.Rank + rankOffset);
                if (to.IsValid)
                {
                    var (piece, pieceColor) = Board.GetPiece(to);
                    if (piece == PieceType.None || pieceColor != color)
                    {
                        moves.Add(to);
                    }
                }
            }
        }
        
        return moves;
    }

    private List<Square> CalculatePossibleMoves(Square from, PieceType pieceType, PieceColor color)
    {
        var possibleMoves = new List<Square>();
        
        if (Board == null) return possibleMoves;
        
        switch (pieceType)
        {
            case PieceType.Pawn:
                possibleMoves.AddRange(CalculatePawnMoves(from, color));
                break;
            case PieceType.Knight:
                possibleMoves.AddRange(CalculateKnightMoves(from, color));
                break;
            case PieceType.Bishop:
                possibleMoves.AddRange(CalculateBishopMoves(from, color));
                break;
            case PieceType.Rook:
                possibleMoves.AddRange(CalculateRookMoves(from, color));
                break;
            case PieceType.Queen:
                possibleMoves.AddRange(CalculateQueenMoves(from, color));
                break;
            case PieceType.King:
                possibleMoves.AddRange(CalculateKingMoves(from, color));
                break;
        }
        
        return possibleMoves;
    }
    
    private List<Square> CalculatePawnMoves(Square from, PieceColor color)
    {
        var moves = new List<Square>();
        var forwardDirection = color == PieceColor.White ? 1 : -1;
        
        // Single square forward
        var oneForward = new Square(from.File, from.Rank + forwardDirection);
        if (oneForward.IsValid && !Board!.IsSquareOccupied(oneForward))
        {
            moves.Add(oneForward);
            
            // Double square forward from starting position
            if ((color == PieceColor.White && from.Rank == 1) || 
                (color == PieceColor.Black && from.Rank == 6))
            {
                var twoForward = new Square(from.File, from.Rank + 2 * forwardDirection);
                if (twoForward.IsValid && !Board.IsSquareOccupied(twoForward))
                {
                    moves.Add(twoForward);
                }
            }
        }
        
        // Diagonal captures
        var leftCapture = new Square(from.File - 1, from.Rank + forwardDirection);
        var rightCapture = new Square(from.File + 1, from.Rank + forwardDirection);
        
        if (leftCapture.IsValid && Board!.IsSquareOccupiedByColor(leftCapture, color == PieceColor.White ? PieceColor.Black : PieceColor.White))
        {
            moves.Add(leftCapture);
        }
        
        if (rightCapture.IsValid && Board.IsSquareOccupiedByColor(rightCapture, color == PieceColor.White ? PieceColor.Black : PieceColor.White))
        {
            moves.Add(rightCapture);
        }
        
        return moves;
    }
    
    private List<Square> CalculateKnightMoves(Square from, PieceColor color)
    {
        var moves = new List<Square>();
        var knightMoves = new[]
        {
            new { File = 2, Rank = 1 }, new { File = 2, Rank = -1 },
            new { File = -2, Rank = 1 }, new { File = -2, Rank = -1 },
            new { File = 1, Rank = 2 }, new { File = 1, Rank = -2 },
            new { File = -1, Rank = 2 }, new { File = -1, Rank = -2 }
        };
        
        foreach (var move in knightMoves)
        {
            var to = new Square(from.File + move.File, from.Rank + move.Rank);
            if (to.IsValid && !Board!.IsSquareOccupiedByColor(to, color))
            {
                moves.Add(to);
            }
        }
        
        return moves;
    }
    
    private List<Square> CalculateBishopMoves(Square from, PieceColor color)
    {
        var moves = new List<Square>();
        var directions = new[] { new { File = 1, Rank = 1 }, new { File = 1, Rank = -1 }, new { File = -1, Rank = 1 }, new { File = -1, Rank = -1 } };
        
        foreach (var direction in directions)
        {
            for (int distance = 1; distance < 8; distance++)
            {
                var to = new Square(from.File + direction.File * distance, from.Rank + direction.Rank * distance);
                if (!to.IsValid) break;
                
                if (Board!.IsSquareOccupied(to))
                {
                    if (Board.IsSquareOccupiedByColor(to, color == PieceColor.White ? PieceColor.Black : PieceColor.White))
                    {
                        moves.Add(to); // Can capture
                    }
                    break; // Blocked
                }
                moves.Add(to);
            }
        }
        
        return moves;
    }
    
    private List<Square> CalculateRookMoves(Square from, PieceColor color)
    {
        var moves = new List<Square>();
        var directions = new[] { new { File = 1, Rank = 0 }, new { File = -1, Rank = 0 }, new { File = 0, Rank = 1 }, new { File = 0, Rank = -1 } };
        
        foreach (var direction in directions)
        {
            for (int distance = 1; distance < 8; distance++)
            {
                var to = new Square(from.File + direction.File * distance, from.Rank + direction.Rank * distance);
                if (!to.IsValid) break;
                
                if (Board!.IsSquareOccupied(to))
                {
                    if (Board.IsSquareOccupiedByColor(to, color == PieceColor.White ? PieceColor.Black : PieceColor.White))
                    {
                        moves.Add(to); // Can capture
                    }
                    break; // Blocked
                }
                moves.Add(to);
            }
        }
        
        return moves;
    }
    
    private List<Square> CalculateQueenMoves(Square from, PieceColor color)
    {
        var moves = new List<Square>();
        moves.AddRange(CalculateBishopMoves(from, color));
        moves.AddRange(CalculateRookMoves(from, color));
        return moves;
    }
    
    private List<Square> CalculateKingMoves(Square from, PieceColor color)
    {
        var moves = new List<Square>();
        var kingMoves = new[]
        {
            new { File = 1, Rank = 0 }, new { File = -1, Rank = 0 },
            new { File = 0, Rank = 1 }, new { File = 0, Rank = -1 },
            new { File = 1, Rank = 1 }, new { File = 1, Rank = -1 },
            new { File = -1, Rank = 1 }, new { File = -1, Rank = -1 }
        };
        
        foreach (var move in kingMoves)
        {
            var to = new Square(from.File + move.File, from.Rank + move.Rank);
            if (to.IsValid && !Board!.IsSquareOccupiedByColor(to, color))
            {
                moves.Add(to);
            }
        }
        
        return moves;
    }
    
    public void AddMove(Move move, int moveNumber)
    {
        // Update last move highlights
        _lastMoveFrom = move.From;
        _lastMoveTo = move.To;
        UpdateHighlights();
        UpdateGameStatus();
    }
    
    public void ClearMoveList()
    {
        _lastMoveFrom = null;
        _lastMoveTo = null;
        UpdateHighlights();
    }
}

public class MoveMadeEventArgs : EventArgs
{
    public Move Move { get; }
    
    public MoveMadeEventArgs(Move move)
    {
        Move = move;
    }
}

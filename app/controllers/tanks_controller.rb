class TanksController < ApplicationController

	def index
		@q = Tank.ransack(params[:q])
		@tanks = @q.result.includes(:marks, :nation, :vehicle_type).paginate(:page => params[:page], :per_page => 20)
	end
	
	def index_winrate
		@q = Tank.ransack(params[:q])
		@tanks = @q.result.includes(:marks, :nation, :vehicle_type).paginate(:page => params[:page], :per_page => 20)
		@tanks_calculation = @q.result.joins("INNER JOIN marks ON tanks.id=marks.tank_id").joins("INNER JOIN players ON marks.player_id=players.id");
	end
	
	def index_wn8
		@q = Tank.ransack(params[:q])
		@tanks = @q.result.includes(:marks, :nation, :vehicle_type).paginate(:page => params[:page], :per_page => 20)
		@tanks_calculation = @q.result.joins("INNER JOIN marks ON tanks.id=marks.tank_id").joins("INNER JOIN players ON marks.player_id=players.id");
	end
	
	def show
		@tank = Tank.find(params[:id])
		
		@q = @tank.marks.ransack(params[:q])
		@marks = @q.result.distinct.paginate(:page => params[:page], :per_page => 20)
		
		rescue ActiveRecord::RecordNotFound
			redirect_to tanks_path and return
	end

end